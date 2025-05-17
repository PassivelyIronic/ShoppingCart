using MediatR;
using ShoppingCart.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingCart.CQRS.Commands.Handlers
{
    public class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, Unit>
    {
        private readonly CartRepository _repo;

        public CheckoutCartCommandHandler(CartRepository repo)
        {
            _repo = repo;
        }

        public async Task<Unit> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
        {
            var cart = await _repo.GetByUserIdAsync(request.UserId);
            if (cart == null || cart.IsCheckedOut)
                throw new System.Exception("Cart not found or already checked out.");

            cart.IsCheckedOut = true;
            await _repo.UpdateAsync(cart);

            // Tu można dodać dalszą logikę (np. wysłanie eventu do kolejki)

            return Unit.Value;
        }
    }
}
