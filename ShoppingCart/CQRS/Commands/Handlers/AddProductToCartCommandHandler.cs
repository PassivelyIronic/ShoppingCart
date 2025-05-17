using MediatR;
using ShoppingCart.Repositories;
using ShoppingCart.Services;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingCart.CQRS.Commands.Handlers
{
    public class AddProductToCartCommandHandler : IRequestHandler<AddProductToCartCommand, Unit>
    {
        private readonly CartRepository _repo;
        private readonly ProductService _productService;

        public AddProductToCartCommandHandler(CartRepository repo, ProductService productService)
        {
            _repo = repo;
            _productService = productService;
        }

        public async Task<Unit> Handle(AddProductToCartCommand request, CancellationToken cancellationToken)
        {
            var cart = await _repo.GetByUserIdAsync(request.UserId);
            if (cart == null || cart.IsCheckedOut)
                throw new System.Exception("Cart not found or already checked out.");

            var product = await _productService.GetProductByIdAsync(request.ProductId);


            var item = cart.Items.Find(i => i.ProductId == request.ProductId);
            if (item != null)
                item.Quantity += request.Quantity;
            else
                cart.Items.Add(new Models.CartItem
                {
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    Price = product.Price,
                    Name = product.Name
                });


            await _repo.UpdateAsync(cart);
            return Unit.Value;
        }
    }
}
