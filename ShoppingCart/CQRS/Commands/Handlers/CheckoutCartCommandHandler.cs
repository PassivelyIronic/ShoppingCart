using MediatR;
using ShoppingCart.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingCart.CQRS.Commands.Handlers
{
    public class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, Unit>
    {
        private readonly CartAggregateRepository _repository;

        public CheckoutCartCommandHandler(CartAggregateRepository repository)
        {
            _repository = repository;
        }

        public async Task<Unit> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
        {
            var aggregate = await _repository.GetByUserIdAsync(request.UserId);
            if (aggregate == null)
                throw new InvalidOperationException("Cart not found");

            // Operacja checkout może być wykonana tylko raz - sprawdzenie w agregacie
            aggregate.Checkout();

            await _repository.SaveAsync(aggregate);

            return Unit.Value;
        }
    }
}