using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using ShoppingCart.Models;
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
    }
}
