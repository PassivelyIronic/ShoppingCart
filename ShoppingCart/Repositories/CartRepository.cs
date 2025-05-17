using MongoDB.Driver;
using ShoppingCart.Models;
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
        public async Task CreateAsync(Cart cart) =>
        await _carts.InsertOneAsync(cart);
        public async Task UpdateAsync(Cart cart) =>
        await _carts.ReplaceOneAsync(c => c.Id == cart.Id, cart);
    }
}