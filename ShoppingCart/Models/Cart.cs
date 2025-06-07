using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShoppingCart.Models
{
    public class Cart
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("items")]
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        [BsonElement("isCheckedOut")]
        public bool IsCheckedOut { get; set; } = false;

        [BsonElement("version")]
        public long Version { get; set; } = 1;

        [BsonElement("lastModified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [BsonIgnore]
        public decimal TotalValue => Items?.Sum(i => i.Price * i.Quantity) ?? 0;
    }
}