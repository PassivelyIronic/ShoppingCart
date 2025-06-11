using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShoppingCart.Events;

namespace ShoppingCart.Models
{
    public class CartAggregateDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } // CartId

        [BsonElement("userId")]
        public string UserId { get; set; }

        [BsonElement("events")]
        public List<CartEvent> Events { get; set; } = new();

        [BsonElement("isCheckedOut")]
        public bool IsCheckedOut { get; set; } = false;

        [BsonElement("version")]
        public long Version { get; set; } = 0;

        [BsonElement("lastModified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;
    }
}