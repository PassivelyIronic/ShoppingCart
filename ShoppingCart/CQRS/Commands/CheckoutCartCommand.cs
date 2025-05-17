using MediatR;

namespace ShoppingCart.CQRS.Commands
{
    public class CheckoutCartCommand : IRequest<Unit>
    {
        public string UserId { get; set; }
    }
}
