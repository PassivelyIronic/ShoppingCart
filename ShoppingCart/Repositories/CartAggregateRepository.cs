using MongoDB.Driver;
using ShoppingCart.Events;
using ShoppingCart.Models;
using System.Threading.Tasks;

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
            // Znajdź aktywny koszyk użytkownika (niesprawdzony)
            var allEvents = await _eventStore.GetEventsForCartAsync($"user-{userId}");

            if (!allEvents.Any())
                return null;

            // Znajdź ostatni aktywny koszyk
            var cartEvents = allEvents.Where(e => !IsCartCheckedOut(allEvents, e.CartId)).ToList();

            if (!cartEvents.Any())
                return null;

            return CartAggregate.FromEvents(cartEvents);
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

                // Opcjonalnie: zapisz snapshot co określoną liczbę wydarzeń
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
                CartId = aggregate.Id,
                UserId = aggregate.UserId,
                Items = aggregate.Items,
                IsCheckedOut = aggregate.IsCheckedOut,
                Version = aggregate.Version,
                Timestamp = DateTime.UtcNow
            };

            var filter = Builders<CartSnapshot>.Filter.Eq(s => s.CartId, aggregate.Id);
            await _snapshots.ReplaceOneAsync(filter, snapshot, new ReplaceOptions { IsUpsert = true });
        }

        private bool IsCartCheckedOut(List<CartEvent> events, string cartId)
        {
            return events.Any(e => e.CartId == cartId && e is CartCheckedOutEvent);
        }
    }

    // ShoppingCart/Models/CartSnapshot.cs - do optymalizacji odczytu
    public class CartSnapshot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string CartId { get; set; }
        public string UserId { get; set; }
        public List<CartItem> Items { get; set; } = new();
        public bool IsCheckedOut { get; set; }
        public long Version { get; set; }
        public DateTime Timestamp { get; set; }
    }
}