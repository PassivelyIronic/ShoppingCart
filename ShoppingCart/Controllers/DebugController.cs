using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ShoppingCart.Models;
using System.Threading.Tasks;

namespace ShoppingCart.Controllers
{
    [ApiController]
    [Route("api/debug")]
    public class DebugController : ControllerBase
    {
        private readonly IMongoDatabase _database;

        public DebugController(IMongoDatabase database)
        {
            _database = database;
        }

        [HttpGet("carts")]
        public async Task<IActionResult> GetAllCarts()
        {
            var collection = _database.GetCollection<Cart>("carts");
            var carts = await collection.Find(_ => true).ToListAsync();

            return Ok(new
            {
                Count = carts.Count,
                Carts = carts.Select(c => new {
                    c.Id,
                    c.UserId,
                    c.IsCheckedOut,
                    c.Version,
                    c.LastModified,
                    ItemCount = c.Items.Count
                })
            });
        }

        [HttpGet("carts/{userId}")]
        public async Task<IActionResult> GetCartsByUserId(string userId)
        {
            var collection = _database.GetCollection<Cart>("carts");
            var carts = await collection.Find(c => c.UserId == userId).ToListAsync();

            return Ok(new
            {
                UserId = userId,
                Count = carts.Count,
                Carts = carts.Select(c => new {
                    c.Id,
                    c.UserId,
                    c.IsCheckedOut,
                    c.Version,
                    c.LastModified,
                    ItemCount = c.Items.Count
                })
            });
        }

        [HttpDelete("carts")]
        public async Task<IActionResult> DeleteAllCarts()
        {
            var collection = _database.GetCollection<Cart>("carts");
            var result = await collection.DeleteManyAsync(_ => true);

            return Ok(new { DeletedCount = result.DeletedCount });
        }
    }
}