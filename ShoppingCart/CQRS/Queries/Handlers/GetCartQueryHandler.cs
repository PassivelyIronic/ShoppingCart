using MediatR;
using ShoppingCart.Models;
using ShoppingCart.Repositories;
using System.Threading;
using System.Threading.Tasks;
namespace ShoppingCart.CQRS.Queries.Handlers
{
    public class GetCartQueryHandler : IRequestHandler<GetCartQuery, Cart>
    {
        private readonly CartRepository _repo;
        public GetCartQueryHandler(CartRepository repo)
        {
            _repo = repo;
        }
        public async Task<Cart> Handle(GetCartQuery request, CancellationToken cancellationToken)
        {
            return await _repo.GetByUserIdAsync(request.UserId);
        }
    }
}