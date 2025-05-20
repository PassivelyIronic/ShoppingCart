using MediatR;
using ShoppingCart.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingCart.CQRS.Commands.Handlers
{
    public class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, Unit>
    {
        private readonly CartRepository _repo;
        private readonly int _maxRetries = 3;

        public CheckoutCartCommandHandler(CartRepository repo)
        {
            _repo = repo;
        }

        public async Task<Unit> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
        {
            int attempts = 0;
            bool updateSuccessful = false;

            while (!updateSuccessful && attempts < _maxRetries)
            {
                attempts++;

                var cart = await _repo.GetByUserIdAsync(request.UserId);
                if (cart == null)
                    throw new Exception("Cart not found.");

                if (cart.IsCheckedOut)
                    throw new Exception("Cart is already checked out.");

                if (cart.Items.Count == 0)
                    throw new Exception("Cannot checkout an empty cart.");

                cart.IsCheckedOut = true;

                updateSuccessful = await _repo.UpdateAsync(cart);

                if (!updateSuccessful && attempts < _maxRetries)
                {
                    await Task.Delay(50 * attempts, cancellationToken);
                }
            }

            if (!updateSuccessful)
                throw new Exception("Failed to checkout cart due to concurrent modifications. Please try again.");

            return Unit.Value;
        }
    }
}