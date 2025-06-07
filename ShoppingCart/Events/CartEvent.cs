using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShoppingCart.Events
{
    public abstract class CartEvent
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string CartId { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public long SequenceNumber { get; set; }
        public string EventType { get; set; }

        protected CartEvent()
        {
            EventType = GetType().Name;
            // Upewnij się, że ID jest zawsze ustawione
            if (string.IsNullOrEmpty(Id))
            {
                Id = Guid.NewGuid().ToString();
            }
        }
    }
}