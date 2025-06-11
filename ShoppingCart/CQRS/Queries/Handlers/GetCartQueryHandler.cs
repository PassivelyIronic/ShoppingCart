using MediatR;
using ShoppingCart.Models;
using ShoppingCart.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingCart.CQRS.Queries.Handlers
{
    public class GetCartQueryHandler : IRequestHandler<GetCartQuery, Cart>
    {
        private readonly CartAggregateRepository _repository;

        public GetCartQueryHandler(CartAggregateRepository repository)
        {
            _repository = repository;
        }

        public async Task<Cart> Handle(GetCartQuery request, CancellationToken cancellationToken)
        {
            var aggregate = await _repository.GetByUserIdAsync(request.UserId);

            if (aggregate == null)
                return null;

            return new Cart
            {
                Id = aggregate.Id,
                UserId = aggregate.UserId,
                Items = aggregate.Items,
                IsCheckedOut = aggregate.IsCheckedOut,
                Version = aggregate.Version,
                LastModified = DateTime.UtcNow
            };
        }
    }
}