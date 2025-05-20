using MongoDB.Driver;
using ShoppingCart.Models;
using System;
using System.Threading.Tasks;

namespace ShoppingCart.Repositories
{
    public class CartRepository
    {
        private readonly IMongoCollection<Cart> _carts;

        public CartRepository(IMongoDatabase db)
        {
            _carts = db.GetCollection<Cart>("carts");
        }

        public async Task<Cart> GetByUserIdAsync(string userId) =>
            await _carts.Find(c => c.UserId == userId && !c.IsCheckedOut).FirstOrDefaultAsync();

        public async Task<Cart> GetByIdAsync(string cartId) =>
            await _carts.Find(c => c.Id == cartId).FirstOrDefaultAsync();

        public async Task CreateAsync(Cart cart)
        {
            cart.Version = 1;
            cart.LastModified = DateTime.UtcNow;
            await _carts.InsertOneAsync(cart);
        }

        public async Task<bool> UpdateAsync(Cart cart)
        {
            long currentVersion = cart.Version;

            cart.Version++;
            cart.LastModified = DateTime.UtcNow;

            var filter = Builders<Cart>.Filter.And(
                Builders<Cart>.Filter.Eq(c => c.Id, cart.Id),
                Builders<Cart>.Filter.Eq(c => c.Version, currentVersion)
            );

            var result = await _carts.ReplaceOneAsync(filter, cart);

            return result.ModifiedCount > 0;
        }
    }
}