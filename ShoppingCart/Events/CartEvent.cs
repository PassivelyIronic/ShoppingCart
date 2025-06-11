using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShoppingCart.Events
{
    public abstract class CartEvent
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } // POPRAWKA: Usuń inicjalizację tutaj

        public string CartId { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public long SequenceNumber { get; set; }
        public string EventType { get; set; }

        protected CartEvent()
        {
            EventType = GetType().Name;
            // POPRAWKA: Nie ustawiaj ID w konstruktorze
            // ID będzie ustawiane przez EventStore przed zapisem
        }
    }
}