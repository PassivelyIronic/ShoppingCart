using MediatR;
namespace ShoppingCart.CQRS.Commands
{
    public class CreateCartCommand : IRequest<string>
    {
        public string UserId { get; set; }
    }
}