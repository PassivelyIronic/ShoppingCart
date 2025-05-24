using Moq;
using ShoppingCart.CQRS.Commands;
using ShoppingCart.CQRS.Commands.Handlers;
using ShoppingCart.Models;
using ShoppingCart.Repositories;
using ShoppingCart.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ShoppingCart.Tests.CQRS.Commands.Handlers
{
    public class AddProductToCartCommandHandlerTests
    {
        private readonly Mock<CartRepository> _repoMock;
        private readonly Mock<ProductService> _productServiceMock;
        private readonly AddProductToCartCommandHandler _handler;

        public AddProductToCartCommandHandlerTests()
        {
            _repoMock = new Mock<CartRepository>();
            _productServiceMock = new Mock<ProductService>();
            _handler = new AddProductToCartCommandHandler(_repoMock.Object, _productServiceMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidProductAndCart_AddsProductToCart()
        {
            // Arrange
            var userId = "user123";
            var productId = "1";
            var cart = new Cart
            {
                Id = "cart456",
                UserId = userId,
                Version = 1
            };

            var product = new ProductDto
            {
                Id = productId,
                Name = "Test Product",
                Price = 10.0m
            };

            var command = new AddProductToCartCommand
            {
                UserId = userId,
                ProductId = productId,
                Quantity = 2
            };

            _repoMock.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(cart);

            _productServiceMock.Setup(p => p.GetProductByIdAsync(productId))
                .ReturnsAsync(product);

            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Cart>()))
                .ReturnsAsync(true);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _repoMock.Verify(r => r.UpdateAsync(It.Is<Cart>(c =>
                c.Items.Count == 1 &&
                c.Items[0].ProductId == productId &&
                c.Items[0].Quantity == 2 &&
                c.Items[0].Price == 10.0m)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithExistingProductInCart_IncreasesQuantity()
        {
            // Arrange
            var userId = "user123";
            var productId = "1";
            var cart = new Cart
            {
                Id = "cart456",
                UserId = userId,
                Version = 1
            };

            cart.Items.Add(new CartItem
            {
                ProductId = productId,
                Name = "Test Product",
                Price = 10.0m,
                Quantity = 1
            });

            var product = new ProductDto
            {
                Id = productId,
                Name = "Test Product",
                Price = 10.0m
            };

            var command = new AddProductToCartCommand
            {
                UserId = userId,
                ProductId = productId,
                Quantity = 2
            };

            _repoMock.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(cart);

            _productServiceMock.Setup(p => p.GetProductByIdAsync(productId))
                .ReturnsAsync(product);

            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Cart>()))
                .ReturnsAsync(true);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _repoMock.Verify(r => r.UpdateAsync(It.Is<Cart>(c =>
                c.Items.Count == 1 &&
                c.Items[0].ProductId == productId &&
                c.Items[0].Quantity == 3)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithZeroQuantity_ThrowsArgumentException()
        {
            // Arrange
            var command = new AddProductToCartCommand
            {
                UserId = "user123",
                ProductId = "1",
                Quantity = 0
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithNonExistentCart_ThrowsException()
        {
            // Arrange
            var command = new AddProductToCartCommand
            {
                UserId = "user123",
                ProductId = "1",
                Quantity = 1
            };

            _repoMock.Setup(r => r.GetByUserIdAsync(command.UserId))
                .ReturnsAsync((Cart)null);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _handler.Handle(command, CancellationToken.None));
        }
    }

    public class CheckoutCartCommandHandlerTests
    {
        private readonly Mock<CartRepository> _repoMock;
        private readonly CheckoutCartCommandHandler _handler;

        public CheckoutCartCommandHandlerTests()
        {
            _repoMock = new Mock<CartRepository>();
            _handler = new CheckoutCartCommandHandler(_repoMock.Object);
        }

        [Fact]
        public async Task Handle_WithValidCart_ChecksOutCart()
        {
            // Arrange
            var userId = "user123";
            var cart = new Cart
            {
                Id = "cart456",
                UserId = userId,
                Version = 1
            };

            cart.Items.Add(new CartItem
            {
                ProductId = "1",
                Name = "Test Product",
                Price = 10.0m,
                Quantity = 1
            });

            var command = new CheckoutCartCommand
            {
                UserId = userId
            };

            _repoMock.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(cart);

            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Cart>()))
                .ReturnsAsync(true);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _repoMock.Verify(r => r.UpdateAsync(It.Is<Cart>(c =>
                c.IsCheckedOut == true)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithEmptyCart_ThrowsException()
        {
            // Arrange
            var userId = "user123";
            var cart = new Cart
            {
                Id = "cart456",
                UserId = userId,
                Version = 1
            };

            var command = new CheckoutCartCommand
            {
                UserId = userId
            };

            _repoMock.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(cart);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _handler.Handle(command, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_WithAlreadyCheckedOutCart_ThrowsException()
        {
            // Arrange
            var userId = "user123";
            var cart = new Cart
            {
                Id = "cart456",
                UserId = userId,
                IsCheckedOut = true,
                Version = 1
            };

            cart.Items.Add(new CartItem
            {
                ProductId = "1",
                Name = "Test Product",
                Price = 10.0m,
                Quantity = 1
            });

            var command = new CheckoutCartCommand
            {
                UserId = userId
            };

            _repoMock.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(cart);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _handler.Handle(command, CancellationToken.None));
        }
    }

    public class RemoveProductFromCartCommandHandlerTests
    {
        private readonly Mock<CartRepository> _repoMock;
        private readonly RemoveProductFromCartCommandHandler _handler;

        public RemoveProductFromCartCommandHandlerTests()
        {
            _repoMock = new Mock<CartRepository>();
            _handler = new RemoveProductFromCartCommandHandler(_repoMock.Object);
        }

        [Fact]
        public async Task Handle_WithExistingProductInCart_RemovesProduct()
        {
            // Arrange
            var userId = "user123";
            var productId = "1";
            var cart = new Cart
            {
                Id = "cart456",
                UserId = userId,
                Version = 1
            };

            cart.Items.Add(new CartItem
            {
                ProductId = productId,
                Name = "Test Product",
                Price = 10.0m,
                Quantity = 1
            });

            var command = new RemoveProductFromCartCommand
            {
                UserId = userId,
                ProductId = productId
            };

            _repoMock.Setup(r => r.GetByUserIdAsync(userId))
                .ReturnsAsync(cart);

            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<Cart>()))
                .ReturnsAsync(true);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _repoMock.Verify(r => r.UpdateAsync(It.Is<Cart>(c =>
                c.Items.Count == 0)),
                Times.Once);
        }

        [Fact]
        public async Task Handle_WithEmptyProductId_ThrowsArgumentException()
        {
            // Arrange
            var command = new RemoveProductFromCartCommand
            {
                UserId = "user123",
                ProductId = ""
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _handler.Handle(command, CancellationToken.None));
        }
    }
}