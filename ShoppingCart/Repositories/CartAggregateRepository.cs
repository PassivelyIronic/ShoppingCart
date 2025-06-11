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

            var userIdIndex = Builders<CartAggregateDocument>.IndexKeys.Ascending(x => x.UserId);
            _cartAggregates.Indexes.CreateOne(new CreateIndexModel<CartAggregateDocument>(userIdIndex));
        }

        public async Task<CartAggregate> GetByUserIdAsync(string userId)
        {
            var activeCartDoc = await _cartAggregates
                .Find(x => x.UserId == userId && !x.IsCheckedOut)
                .SortByDescending(x => x.LastModified)
                .FirstOrDefaultAsync();

            if (activeCartDoc != null)
            {
                return CartAggregate.FromEvents(activeCartDoc.Events.OrderBy(e => e.SequenceNumber).ToList());
            }

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
            var cartDoc = await _cartAggregates.Find(x => x.Id == cartId).FirstOrDefaultAsync();
            if (cartDoc != null)
            {
                return CartAggregate.FromEvents(cartDoc.Events.OrderBy(e => e.SequenceNumber).ToList());
            }

            var events = await _eventStore.GetEventsForCartAsync(cartId);
            return CartAggregate.FromEvents(events);
        }

        public async Task SaveAsync(CartAggregate aggregate)
        {
            var uncommittedEvents = aggregate.GetUncommittedEvents();
            if (!uncommittedEvents.Any()) return;

            var isCheckout = uncommittedEvents.Any(e => e is CartCheckedOutEvent);

            if (isCheckout)
            {
                await HandleCheckoutAsync(aggregate, uncommittedEvents);
            }
            else
            {
                await UpdateCartAggregateAsync(aggregate, uncommittedEvents);
            }

            aggregate.MarkEventsAsCommitted();
        }

        private async Task HandleCheckoutAsync(CartAggregate aggregate, List<CartEvent> uncommittedEvents)
        {
            var existingDoc = await _cartAggregates.Find(x => x.Id == aggregate.Id).FirstOrDefaultAsync();

            List<CartEvent> allEvents;
            if (existingDoc != null)
            {
                allEvents = existingDoc.Events.Concat(uncommittedEvents).OrderBy(e => e.SequenceNumber).ToList();
            }
            else
            {
                allEvents = uncommittedEvents;
            }

            await _eventStore.SaveEventsAsync(uncommittedEvents);

            var completeEvents = await _eventStore.GetEventsForCartAsync(aggregate.Id);
            var rebuiltAggregate = CartAggregate.FromEvents(completeEvents);

            var finalCart = new Cart
            {
                Id = rebuiltAggregate.Id,
                UserId = rebuiltAggregate.UserId,
                Items = rebuiltAggregate.Items,
                IsCheckedOut = true,
                Version = rebuiltAggregate.Version,
                LastModified = DateTime.UtcNow
            };

            await _carts.InsertOneAsync(finalCart);

            var finalCartDoc = new CartAggregateDocument
            {
                Id = rebuiltAggregate.Id,
                UserId = rebuiltAggregate.UserId,
                Events = completeEvents,
                IsCheckedOut = true,
                Version = rebuiltAggregate.Version,
                LastModified = DateTime.UtcNow
            };

            if (existingDoc != null)
            {
                await _cartAggregates.ReplaceOneAsync(x => x.Id == rebuiltAggregate.Id, finalCartDoc);
            }
            else
            {
                await _cartAggregates.InsertOneAsync(finalCartDoc);
            }
        }

        private async Task UpdateCartAggregateAsync(CartAggregate aggregate, List<CartEvent> uncommittedEvents)
        {
            await _eventStore.SaveEventsAsync(uncommittedEvents);

            var allEvents = await _eventStore.GetEventsForCartAsync(aggregate.Id);
            var rebuiltAggregate = CartAggregate.FromEvents(allEvents);

            var existingDoc = await _cartAggregates.Find(x => x.Id == aggregate.Id).FirstOrDefaultAsync();

            if (existingDoc != null)
            {
                existingDoc.Events = allEvents;
                existingDoc.Version = rebuiltAggregate.Version;
                existingDoc.LastModified = DateTime.UtcNow;

                await _cartAggregates.ReplaceOneAsync(x => x.Id == aggregate.Id, existingDoc);
            }
            else
            {
                var newDoc = new CartAggregateDocument
                {
                    Id = rebuiltAggregate.Id,
                    UserId = rebuiltAggregate.UserId,
                    Events = allEvents,
                    IsCheckedOut = false,
                    Version = rebuiltAggregate.Version,
                    LastModified = DateTime.UtcNow
                };

                await _cartAggregates.InsertOneAsync(newDoc);
            }
        }

        //nieaktualne
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