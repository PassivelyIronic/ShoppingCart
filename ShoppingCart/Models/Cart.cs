using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace ShoppingCart.Models
{
    public class Cart
    {
        [BsonId]
        public string Id { get; set; }
        public string UserId { get; set; }
        public List<CartItem> Items { get; set; } = new();
        public bool IsCheckedOut { get; set; }
        public long Version { get; set; } = 1;

        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        [BsonIgnore]
        public decimal TotalValue => Items.Sum(i => i.Price * i.Quantity);
    }
}