using MediatR;
namespace ShoppingCart.CQRS.Commands
{
    public class AddProductToCartCommand : IRequest<Unit>
    {
        public string UserId { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}