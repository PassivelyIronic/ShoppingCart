using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ShoppingCart.Controllers;
using ShoppingCart.CQRS.Commands;
using ShoppingCart.CQRS.Queries;
using ShoppingCart.Models;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ShoppingCart.Tests.Controllers
{
    public class CartControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly CartController _controller;

        public CartControllerTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _controller = new CartController(_mediatorMock.Object);
        }

        [Fact]
        public async Task Create_WithValidUserId_ReturnsOkWithCartId()
        {
            // Arrange
            var userId = "user123";
            var expectedCartId = "cart456"; //tu mongo robi nowe bson id wiec na bank nie będzie takie to ma wgl inny format

            _mediatorMock
                .Setup(m => m.Send(It.Is<CreateCartCommand>(c => c.UserId == userId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedCartId);

            // Act
            var result = await _controller.Create(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic value = okResult.Value;
            Assert.Equal(expectedCartId, value.CartId.ToString());
        }

        [Fact]
        public async Task Create_WithEmptyUserId_ReturnsBadRequest()
        {
            // Arrange
            string userId = "";

            // Act
            var result = await _controller.Create(userId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Get_WithExistingCart_ReturnsOkWithCart()
        {
            // Arrange
            var userId = "user123";
            var cart = new Cart
            { 
                UserId = userId
            };
            cart.Items.Add(new CartItem { ProductId = "1", Name = "Test Product", Price = 10.0m, Quantity = 2 });

            _mediatorMock
                .Setup(m => m.Send(It.Is<GetCartQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cart);

            // Act
            var result = await _controller.Get(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            dynamic value = okResult.Value;
            Assert.Equal(cart.TotalValue, (decimal)value.TotalValue);
        }

        [Fact]
        public async Task Get_WithNonExistingCart_ReturnsNotFound()
        {
            // Arrange
            var userId = "nonexistent";

            _mediatorMock
                .Setup(m => m.Send(It.Is<GetCartQuery>(q => q.UserId == userId), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Cart)null);

            // Act
            var result = await _controller.Get(userId);

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task Add_WithValidCommand_ReturnsOk()
        {
            // Arrange
            var command = new AddProductToCartCommand
            {
                UserId = "user123",
                ProductId = "1",
                Quantity = 2
            };

            _mediatorMock
                .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.Add(command);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Add_WithInvalidCommand_ReturnsBadRequest()
        {
            // Arrange
            var command = new AddProductToCartCommand
            {
                UserId = "user123",
                ProductId = "1",
                Quantity = 0 // Invalid quantity
            };

            // Act
            var result = await _controller.Add(command);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Remove_WithValidCommand_ReturnsOk()
        {
            // Arrange
            var command = new RemoveProductFromCartCommand
            {
                UserId = "user123",
                ProductId = "1"
            };

            _mediatorMock
                .Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.Remove(command);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Checkout_WithValidUserId_ReturnsOk()
        {
            // Arrange
            var userId = "user123";

            _mediatorMock
                .Setup(m => m.Send(It.Is<CheckoutCartCommand>(c => c.UserId == userId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.Checkout(userId);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Checkout_WithEmptyUserId_ReturnsBadRequest()
        {
            // Arrange
            string userId = "";

            // Act
            var result = await _controller.Checkout(userId);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}