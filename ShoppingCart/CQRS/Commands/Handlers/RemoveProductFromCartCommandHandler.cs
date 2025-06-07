using MediatR;
using ShoppingCart.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingCart.CQRS.Commands.Handlers
{
    public class RemoveProductFromCartCommandHandler : IRequestHandler<RemoveProductFromCartCommand, Unit>
    {
        private readonly CartAggregateRepository _repository;

        public RemoveProductFromCartCommandHandler(CartAggregateRepository repository)
        {
            _repository = repository;
        }

        public async Task<Unit> Handle(RemoveProductFromCartCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.ProductId))
                throw new ArgumentException("Product ID cannot be empty", nameof(request.ProductId));

            var aggregate = await _repository.GetByUserIdAsync(request.UserId);
            if (aggregate == null)
                throw new InvalidOperationException("Cart not found");

            // Event Sourcing - operacja jest zawsze wykonywana
            aggregate.RemoveProduct(request.ProductId);

            await _repository.SaveAsync(aggregate);

            return Unit.Value;
        }
    }
}