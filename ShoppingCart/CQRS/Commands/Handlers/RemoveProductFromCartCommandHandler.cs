using MediatR;
using ShoppingCart.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingCart.CQRS.Commands.Handlers
{
    public class RemoveProductFromCartCommandHandler : IRequestHandler<RemoveProductFromCartCommand, Unit>
    {
        private readonly CartRepository _repo;

        public RemoveProductFromCartCommandHandler(CartRepository repo)
        {
            _repo = repo;
        }

        public async Task<Unit> Handle(RemoveProductFromCartCommand request, CancellationToken cancellationToken)
        {
            var cart = await _repo.GetByUserIdAsync(request.UserId);
            if (cart == null || cart.IsCheckedOut)
                throw new System.Exception("Cart not found or already checked out.");

            cart.Items.RemoveAll(i => i.ProductId == request.ProductId);
            await _repo.UpdateAsync(cart);

            return Unit.Value;
        }
    }
}
