using MediatR;
using ShoppingCart.Models;
namespace ShoppingCart.CQRS.Queries
{
    public class GetCartQuery : IRequest<Cart>
    {
        public string UserId { get; set; }
    }
}