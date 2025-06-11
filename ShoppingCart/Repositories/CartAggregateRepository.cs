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

        public CartAggregateRepository(EventStore eventStore, IMongoDatabase database)
        {
            _eventStore = eventStore;
            _snapshots = database.GetCollection<CartSnapshot>("snapshots");
        }

        public async Task<CartAggregate> GetByUserIdAsync(string userId)
        {
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
            var events = await _eventStore.GetEventsForCartAsync(cartId);
            return CartAggregate.FromEvents(events);
        }

        public async Task SaveAsync(CartAggregate aggregate)
        {
            var uncommittedEvents = aggregate.GetUncommittedEvents();
            if (uncommittedEvents.Any())
            {
                await _eventStore.SaveEventsAsync(uncommittedEvents);
                aggregate.MarkEventsAsCommitted();

                if (aggregate.Version % 10 == 0)
                {
                    await SaveSnapshotAsync(aggregate);
                }
            }
        }

        private async Task SaveSnapshotAsync(CartAggregate aggregate)
        {
            var snapshot = new CartSnapshot
            {
                // POPRAWKA: Nie ustawiaj Id tutaj - pozwól MongoDB wygenerować
                CartId = aggregate.Id,
                UserId = aggregate.UserId,
                Items = aggregate.Items,
                IsCheckedOut = aggregate.IsCheckedOut,
                Version = aggregate.Version,
                Timestamp = DateTime.UtcNow
            };

            var filter = Builders<CartSnapshot>.Filter.Eq(s => s.CartId, aggregate.Id);

            // POPRAWKA: Użyj bardziej bezpiecznej operacji
            try
            {
                var existing = await _snapshots.Find(filter).FirstOrDefaultAsync();
                if (existing != null)
                {
                    // Jeśli istnieje, użyj jego ID
                    snapshot.Id = existing.Id;
                    await _snapshots.ReplaceOneAsync(filter, snapshot);
                }
                else
                {
                    // Jeśli nie istnieje, wstaw nowy (MongoDB wygeneruje ID)
                    snapshot.Id = null; // Pozwól MongoDB wygenerować
                    await _snapshots.InsertOneAsync(snapshot);
                }
            }
            catch (MongoWriteException ex) when (ex.WriteError.Code == 11000) // Duplicate key error
            {
                // Jeśli wystąpi konflikt, spróbuj ponownie z replace
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
        public string Id { get; set; } // POPRAWKA: Usuń inicjalizację - pozwól MongoDB zarządzać

        public string CartId { get; set; }
        public string UserId { get; set; }
        public List<CartItem> Items { get; set; } = new();
        public bool IsCheckedOut { get; set; }
        public long Version { get; set; }
        public DateTime Timestamp { get; set; }
    }
}