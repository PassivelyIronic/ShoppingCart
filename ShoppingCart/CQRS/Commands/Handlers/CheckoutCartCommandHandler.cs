using MediatR;
using ShoppingCart.Repositories;
using ShoppingCart.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingCart.CQRS.Commands.Handlers
{
    public class CheckoutCartCommandHandler : IRequestHandler<CheckoutCartCommand, Unit>
    {
        private readonly CartAggregateRepository _repository;
        private readonly ProductService _productService;

        public CheckoutCartCommandHandler(CartAggregateRepository repository, ProductService productService)
        {
            _repository = repository;
            _productService = productService;
        }

        public async Task<Unit> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
        {
            var aggregate = await _repository.GetByUserIdAsync(request.UserId);
            if (aggregate == null)
                throw new InvalidOperationException("Cart not found");

            foreach (var item in aggregate.Items)
            {
                try
                {
                    var product = await _productService.GetProductByIdAsync(item.ProductId);
                    // Sprawdź czy cena się nie zmieniła znacząco
                    if (Math.Abs(product.Price - item.Price) > 0.01m)
                    {
                        throw new InvalidOperationException(
                            $"Price for product {item.Name} has changed. Please update your cart.");
                    }
                }
                catch (Exception ex) when (ex.Message.Contains("not found"))
                {
                    throw new InvalidOperationException(
                        $"Product {item.Name} is no longer available. Please remove it from cart.");
                }
            }

            // Operacja checkout może być wykonana tylko raz
            aggregate.Checkout();

            await _repository.SaveAsync(aggregate);

            return Unit.Value;
        }
    }
}