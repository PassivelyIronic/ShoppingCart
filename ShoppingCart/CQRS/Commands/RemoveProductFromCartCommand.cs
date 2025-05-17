using MediatR;

namespace ShoppingCart.CQRS.Commands
{
    public class RemoveProductFromCartCommand : IRequest<Unit>
    {
        public string UserId { get; set; }
        public string ProductId { get; set; }
    }
}
