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
            var cartId = await _mediator.Send(new CreateCartCommand { UserId = userId });
            return Ok(cartId);
        }
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] AddProductToCartCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok();
        }
        [HttpPost("remove")]
        public async Task<IActionResult> Remove([FromBody] RemoveProductFromCartCommand cmd)
        {
            await _mediator.Send(cmd);
            return Ok();
        }
        [HttpGet("{userId}")]
        public async Task<IActionResult> Get(string userId)
        {
            var cart = await _mediator.Send(new GetCartQuery { UserId = userId });
            if (cart == null) return NotFound();
            return Ok(cart);
        }
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] string userId)
        {
            await _mediator.Send(new CheckoutCartCommand { UserId = userId });
            return Ok();
        }
    }
}