using MediatR;
using Microsoft.AspNetCore.Mvc;
using ShoppingCart.CQRS.Commands;
using ShoppingCart.CQRS.Queries;
using System.Threading.Tasks;

namespace ShoppingCart.Controllers
{
    [ApiController]
    [Route("api/cart")]
    public class CartController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CartController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID is required");

            var cartId = await _mediator.Send(new CreateCartCommand { UserId = userId });
            return Ok(new { CartId = cartId });
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddProductToCartCommand cmd)
        {
            if (string.IsNullOrEmpty(cmd.UserId) || string.IsNullOrEmpty(cmd.ProductId) || cmd.Quantity <= 0)
                return BadRequest("Valid User ID, Product ID and positive Quantity are required");

            try
            {
                await _mediator.Send(cmd);
                return Ok(new { Message = "Product added to cart successfully" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("remove")]
        public async Task<IActionResult> Remove([FromBody] RemoveProductFromCartCommand cmd)
        {
            if (string.IsNullOrEmpty(cmd.UserId) || string.IsNullOrEmpty(cmd.ProductId))
                return BadRequest("Valid User ID and Product ID are required");

            try
            {
                await _mediator.Send(cmd);
                return Ok(new { Message = "Product removed from cart successfully" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> Get(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID is required");

            var cart = await _mediator.Send(new GetCartQuery { UserId = userId });
            if (cart == null)
                return NotFound(new { Error = "Cart not found" });

            return Ok(new
            {
                Cart = cart,
                TotalValue = cart.TotalValue
            });
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID is required");

            try
            {
                await _mediator.Send(new CheckoutCartCommand { UserId = userId });
                return Ok(new { Message = "Cart checkout successful" });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}