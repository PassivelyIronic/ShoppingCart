using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace ShoppingCart.Events
{
    public class EventStore
    {
        private readonly IMongoCollection<CartEvent> _events;

        public EventStore(IMongoDatabase database)
        {
            _events = database.GetCollection<CartEvent>("events");

            var indexKeysDefinition = Builders<CartEvent>.IndexKeys
                .Ascending(e => e.CartId)
                .Ascending(e => e.SequenceNumber);
            _events.Indexes.CreateOne(new CreateIndexModel<CartEvent>(indexKeysDefinition));

            var userIndexKeys = Builders<CartEvent>.IndexKeys.Ascending(e => e.UserId);
            _events.Indexes.CreateOne(new CreateIndexModel<CartEvent>(userIndexKeys));
        }

        public async Task<List<CartEvent>> GetEventsForCartAsync(string cartId)
        {
            return await _events.Find(e => e.CartId == cartId)
                .SortBy(e => e.SequenceNumber)
                .ToListAsync();
        }

        public async Task<List<CartEvent>> GetEventsByUserIdAsync(string userId)
        {
            return await _events.Find(e => e.UserId == userId)
                .SortBy(e => e.Timestamp)
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
            cartEvent.Id = Guid.NewGuid().ToString();
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
                evt.Id = Guid.NewGuid().ToString();
                evt.SequenceNumber = nextSequence++;
            }

            try
            {
                await _events.InsertManyAsync(events);
            }
            catch (MongoBulkWriteException ex)
            {
                var details = string.Join(", ", ex.WriteErrors.Select(e => $"Index: {e.Index}, Code: {e.Code}, Message: {e.Message}"));
                throw new InvalidOperationException($"Failed to save events. Details: {details}", ex);
            }
            catch (MongoWriteException ex)
            {
                throw new InvalidOperationException($"Failed to save event: Code {ex.WriteError.Code}, Message: {ex.WriteError.Message}", ex);
            }
        }
    }
}