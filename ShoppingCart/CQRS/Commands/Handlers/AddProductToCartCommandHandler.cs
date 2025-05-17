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
        private readonly CartRepository _repo;
        private readonly ProductService _productService;
        private readonly int _maxRetries = 3; // Maksymalna liczba prób przy konfliktach

        public AddProductToCartCommandHandler(CartRepository repo, ProductService productService)
        {
            _repo = repo;
            _productService = productService;
        }

        public async Task<Unit> Handle(AddProductToCartCommand request, CancellationToken cancellationToken)
        {
            // Walidacja wejścia
            if (request.Quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero", nameof(request.Quantity));

            int attempts = 0;
            bool updateSuccessful = false;

            // Mechanizm ponownych prób z obsługą konfliktów współbieżności
            while (!updateSuccessful && attempts < _maxRetries)
            {
                attempts++;

                var cart = await _repo.GetByUserIdAsync(request.UserId);
                if (cart == null || cart.IsCheckedOut)
                    throw new Exception("Cart not found or already checked out.");

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

                // Próba aktualizacji z obsługą konfliktów
                updateSuccessful = await _repo.UpdateAsync(cart);

                // Jeśli aktualizacja się nie powiodła, poczekaj krótko przed ponowną próbą
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