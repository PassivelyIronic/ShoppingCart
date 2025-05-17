using MediatR;
using ShoppingCart.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingCart.CQRS.Commands.Handlers
{
    public class RemoveProductFromCartCommandHandler : IRequestHandler<RemoveProductFromCartCommand, Unit>
    {
        private readonly CartRepository _repo;
        private readonly int _maxRetries = 3;

        public RemoveProductFromCartCommandHandler(CartRepository repo)
        {
            _repo = repo;
        }

        public async Task<Unit> Handle(RemoveProductFromCartCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.ProductId))
                throw new ArgumentException("Product ID cannot be empty", nameof(request.ProductId));

            int attempts = 0;
            bool updateSuccessful = false;

            while (!updateSuccessful && attempts < _maxRetries)
            {
                attempts++;

                var cart = await _repo.GetByUserIdAsync(request.UserId);
                if (cart == null || cart.IsCheckedOut)
                    throw new Exception("Cart not found or already checked out.");

                // Usuwamy produkty z koszyka
                cart.Items.RemoveAll(i => i.ProductId == request.ProductId);

                // Próba aktualizacji z obsługą konfliktów
                updateSuccessful = await _repo.UpdateAsync(cart);

                if (!updateSuccessful && attempts < _maxRetries)
                {
                    await Task.Delay(50 * attempts, cancellationToken); // Backoff strategy
                }
            }

            if (!updateSuccessful)
                throw new Exception("Failed to update cart due to concurrent modifications. Please try again.");

            return Unit.Value;
        }
    }
}