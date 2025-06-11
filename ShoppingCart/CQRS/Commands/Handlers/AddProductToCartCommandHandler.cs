using MediatR;
using ShoppingCart.Repositories;
using ShoppingCart.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingCart.CQRS.Commands.Handlers
{
    public class AddProductToCartCommandHandler : IRequestHandler<AddProductToCartCommand, Unit>
    {
        private readonly CartAggregateRepository _repository;
        private readonly ProductService _productService;

        public AddProductToCartCommandHandler(CartAggregateRepository repository, ProductService productService)
        {
            _repository = repository;
            _productService = productService;
        }

        public async Task<Unit> Handle(AddProductToCartCommand request, CancellationToken cancellationToken)
        {
            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero", nameof(request.Quantity));

            var aggregate = await _repository.GetByUserIdAsync(request.UserId);
            if (aggregate == null)
                throw new InvalidOperationException("Cart not found. Please create a cart first.");

            var product = await _productService.GetProductByIdAsync(request.ProductId);

            aggregate.AddProduct(request.ProductId, request.Quantity, product.Price, product.Name);

            await _repository.SaveAsync(aggregate);

            return Unit.Value;
        }
    }
}