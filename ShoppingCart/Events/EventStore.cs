using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShoppingCart.Events
{
    public class EventStore
    {
        private readonly IMongoCollection<CartEvent> _events;

        public EventStore(IMongoDatabase database)
        {
            _events = database.GetCollection<CartEvent>("events");

            // Utworzenie indeksu dla wydajności
            var indexKeysDefinition = Builders<CartEvent>.IndexKeys
                .Ascending(e => e.CartId)
                .Ascending(e => e.SequenceNumber);
            _events.Indexes.CreateOne(new CreateIndexModel<CartEvent>(indexKeysDefinition));
        }

        public async Task<List<CartEvent>> GetEventsForCartAsync(string cartId)
        {
            return await _events.Find(e => e.CartId == cartId)
                .SortBy(e => e.SequenceNumber)
                .ToListAsync();
        }

        public async Task<long> GetNextSequenceNumberAsync(string cartId)
        {
            var lastEvent = await _events.Find(e => e.CartId == cartId)
                .SortByDescending(e => e.SequenceNumber)
                .FirstOrDefaultAsync();

            return lastEvent?.SequenceNumber + 1 ?? 1;
        }

        public async Task SaveEventAsync(CartEvent cartEvent)
        {
            cartEvent.SequenceNumber = await GetNextSequenceNumberAsync(cartEvent.CartId);
            await _events.InsertOneAsync(cartEvent);
        }

        public async Task SaveEventsAsync(List<CartEvent> events)
        {
            if (!events.Any()) return;

            string cartId = events.First().CartId;
            long nextSequence = await GetNextSequenceNumberAsync(cartId);

            foreach (var evt in events)
            {
                evt.SequenceNumber = nextSequence++;
            }

            await _events.InsertManyAsync(events);
        }
    }
}