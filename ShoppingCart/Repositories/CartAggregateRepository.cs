using MongoDB.Driver;
using ShoppingCart.Events;
using ShoppingCart.Models;
using System.Threading.Tasks;
using System.Linq;

namespace ShoppingCart.Repositories
{
    public class CartAggregateRepository
    {
        private readonly EventStore _eventStore;
        private readonly IMongoCollection<CartSnapshot> _snapshots;
        private readonly IMongoCollection<CartAggregateDocument> _cartAggregates;
        private readonly IMongoCollection<Cart> _carts;

        public CartAggregateRepository(EventStore eventStore, IMongoDatabase database)
        {
            _eventStore = eventStore;
            _snapshots = database.GetCollection<CartSnapshot>("snapshots");
            _cartAggregates = database.GetCollection<CartAggregateDocument>("cartaggregate");
            _carts = database.GetCollection<Cart>("carts");

            // Indeksy dla cartAggregate
            var userIdIndex = Builders<CartAggregateDocument>.IndexKeys.Ascending(x => x.UserId);
            _cartAggregates.Indexes.CreateOne(new CreateIndexModel<CartAggregateDocument>(userIdIndex));
        }

        public async Task<CartAggregate> GetByUserIdAsync(string userId)
        {
            // Najpierw sprawdź czy istnieje aktywny cart w cartAggregate (nie checkout)
            var activeCartDoc = await _cartAggregates
                .Find(x => x.UserId == userId && !x.IsCheckedOut)
                .SortByDescending(x => x.LastModified)
                .FirstOrDefaultAsync();

            if (activeCartDoc != null)
            {
                return CartAggregate.FromEvents(activeCartDoc.Events.OrderBy(e => e.SequenceNumber).ToList());
            }

            // Fallback - sprawdź stare eventy w event store
            var allEvents = await _eventStore.GetEventsByUserIdAsync(userId);
            if (!allEvents.Any())
                return null;

            var eventsByCart = allEvents.GroupBy(e => e.CartId).ToList();

            foreach (var cartGroup in eventsByCart.OrderByDescending(g => g.Max(e => e.Timestamp)))
            {
                var cartEvents = cartGroup.OrderBy(e => e.SequenceNumber).ToList();
                var isCheckedOut = cartEvents.Any(e => e is CartCheckedOutEvent);

                if (!isCheckedOut)
                {
                    return CartAggregate.FromEvents(cartEvents);
                }
            }

            return null;
        }

        public async Task<CartAggregate> GetByIdAsync(string cartId)
        {
            // Sprawdź w cartAggregate
            var cartDoc = await _cartAggregates.Find(x => x.Id == cartId).FirstOrDefaultAsync();
            if (cartDoc != null)
            {
                return CartAggregate.FromEvents(cartDoc.Events.OrderBy(e => e.SequenceNumber).ToList());
            }

            // Fallback - sprawdź w event store
            var events = await _eventStore.GetEventsForCartAsync(cartId);
            return CartAggregate.FromEvents(events);
        }

        public async Task SaveAsync(CartAggregate aggregate)
        {
            var uncommittedEvents = aggregate.GetUncommittedEvents();
            if (!uncommittedEvents.Any()) return;

            // Sprawdź czy to checkout
            var isCheckout = uncommittedEvents.Any(e => e is CartCheckedOutEvent);

            if (isCheckout)
            {
                // Pri checkout: zapisz wszystkie eventy w cartAggregate, przenieś do carts, usuń z cartAggregate
                await HandleCheckoutAsync(aggregate, uncommittedEvents);
            }
            else
            {
                // Normalne operacje: aktualizuj cartAggregate
                await UpdateCartAggregateAsync(aggregate, uncommittedEvents);
            }

            aggregate.MarkEventsAsCommitted();
        }

        private async Task HandleCheckoutAsync(CartAggregate aggregate, List<CartEvent> uncommittedEvents)
        {
            // 1. Pobierz istniejący dokument cartAggregate
            var existingDoc = await _cartAggregates.Find(x => x.Id == aggregate.Id).FirstOrDefaultAsync();

            List<CartEvent> allEvents;
            if (existingDoc != null)
            {
                // Połącz stare eventy z nowymi
                allEvents = existingDoc.Events.Concat(uncommittedEvents).OrderBy(e => e.SequenceNumber).ToList();
            }
            else
            {
                // Tylko nowe eventy (nie powinno się zdarzyć, ale na wszelki wypadek)
                allEvents = uncommittedEvents;
            }

            // 2. Zapisz w event store (dla historii)
            await _eventStore.SaveEventsAsync(uncommittedEvents);

            // 3. Utwórz finalne Cart do zapisania w kolekcji carts
            var finalCart = new Cart
            {
                Id = aggregate.Id,
                UserId = aggregate.UserId,
                Items = aggregate.Items,
                IsCheckedOut = true,
                Version = aggregate.Version,
                LastModified = DateTime.UtcNow
            };

            // 4. Zapisz w kolekcji carts
            await _carts.InsertOneAsync(finalCart);

            // 5. Oznacz cartAggregate jako checkout i zachowaj wszystkie eventy
            var finalCartDoc = new CartAggregateDocument
            {
                Id = aggregate.Id,
                UserId = aggregate.UserId,
                Events = allEvents,
                IsCheckedOut = true,
                Version = aggregate.Version,
                LastModified = DateTime.UtcNow
            };

            if (existingDoc != null)
            {
                await _cartAggregates.ReplaceOneAsync(x => x.Id == aggregate.Id, finalCartDoc);
            }
            else
            {
                await _cartAggregates.InsertOneAsync(finalCartDoc);
            }
        }

        private async Task UpdateCartAggregateAsync(CartAggregate aggregate, List<CartEvent> uncommittedEvents)
        {
            // Zapisz eventy w event store
            await _eventStore.SaveEventsAsync(uncommittedEvents);

            // Aktualizuj cartAggregate
            var existingDoc = await _cartAggregates.Find(x => x.Id == aggregate.Id).FirstOrDefaultAsync();

            if (existingDoc != null)
            {
                // Dodaj nowe eventy do istniejących
                existingDoc.Events.AddRange(uncommittedEvents);
                existingDoc.Version = aggregate.Version;
                existingDoc.LastModified = DateTime.UtcNow;

                await _cartAggregates.ReplaceOneAsync(x => x.Id == aggregate.Id, existingDoc);
            }
            else
            {
                // Utwórz nowy dokument
                var newDoc = new CartAggregateDocument
                {
                    Id = aggregate.Id,
                    UserId = aggregate.UserId,
                    Events = uncommittedEvents,
                    IsCheckedOut = false,
                    Version = aggregate.Version,
                    LastModified = DateTime.UtcNow
                };

                await _cartAggregates.InsertOneAsync(newDoc);
            }
        }

        private async Task SaveSnapshotAsync(CartAggregate aggregate)
        {
            var snapshot = new CartSnapshot
            {
                CartId = aggregate.Id,
                UserId = aggregate.UserId,
                Items = aggregate.Items,
                IsCheckedOut = aggregate.IsCheckedOut,
                Version = aggregate.Version,
                Timestamp = DateTime.UtcNow
            };

            var filter = Builders<CartSnapshot>.Filter.Eq(s => s.CartId, aggregate.Id);

            try
            {
                var existing = await _snapshots.Find(filter).FirstOrDefaultAsync();
                if (existing != null)
                {
                    snapshot.Id = existing.Id;
                    await _snapshots.ReplaceOneAsync(filter, snapshot);
                }
                else
                {
                    snapshot.Id = null;
                    await _snapshots.InsertOneAsync(snapshot);
                }
            }
            catch (MongoWriteException ex) when (ex.WriteError.Code == 11000)
            {
                var existingForRetry = await _snapshots.Find(filter).FirstOrDefaultAsync();
                if (existingForRetry != null)
                {
                    snapshot.Id = existingForRetry.Id;
                    await _snapshots.ReplaceOneAsync(filter, snapshot);
                }
            }
        }
    }

    public class CartSnapshot
    {
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        [MongoDB.Bson.Serialization.Attributes.BsonRepresentation(MongoDB.Bson.BsonType.String)]
        public string Id { get; set; }

        public string CartId { get; set; }
        public string UserId { get; set; }
        public List<CartItem> Items { get; set; } = new();
        public bool IsCheckedOut { get; set; }
        public long Version { get; set; }
        public DateTime Timestamp { get; set; }
    }
}