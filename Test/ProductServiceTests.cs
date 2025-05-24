using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;
using ShoppingCart.Models;
using ShoppingCart.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ShoppingCart.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IMemoryCache> _cacheMock;
        private readonly Mock<HttpMessageHandler> _handlerMock;
        private readonly HttpClient _httpClient;
        private readonly ProductService _service;

        public ProductServiceTests()
        {
            _cacheMock = new Mock<IMemoryCache>();
            _handlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_handlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost:4000")
            };

            _service = new ProductService(_httpClient, _cacheMock.Object);
        }

        [Fact]
        public async Task GetProductByIdAsync_WithExistingProduct_ReturnsProduct()
        {
            // Arrange
            var productId = "1";
            var expectedProduct = new ProductDto
            {
                Id = productId,
                Name = "Laptop",
                Price = 2500.00m
            };

            var mockResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(expectedProduct))
            };

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get && r.RequestUri.ToString().Contains(productId)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(mockResponse);

            // Setup cache mock to simulate cache miss
            object outValue;
            _cacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out outValue))
                .Returns(false);

            // Act
            var result = await _service.GetProductByIdAsync(productId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedProduct.Id, result.Id);
            Assert.Equal(expectedProduct.Name, result.Name);
            Assert.Equal(expectedProduct.Price, result.Price);

            // Verify cache was updated
            _cacheMock.Verify(
                m => m.Set(
                    It.Is<string>(s => s == $"product_{productId}"),
                    It.IsAny<ProductDto>(),
                    It.IsAny<TimeSpan>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task GetProductByIdAsync_WithCachedProduct_ReturnsCachedProductWithoutHttpCall()
        {
            // Arrange
            var productId = "1";
            var cachedProduct = new ProductDto
            {
                Id = productId,
                Name = "Laptop",
                Price = 2500.00m
            };

            // Setup cache mock to simulate cache hit
            _cacheMock
                .Setup(m => m.TryGetValue(It.Is<string>(s => s == $"product_{productId}"), out cachedProduct))
                .Returns(true);

            // Act
            var result = await _service.GetProductByIdAsync(productId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cachedProduct.Id, result.Id);
            Assert.Equal(cachedProduct.Name, result.Name);
            Assert.Equal(cachedProduct.Price, result.Price);

            // Verify HTTP client was not called
            _handlerMock
                .Protected()
                .Verify<Task<HttpResponseMessage>>(
                    "SendAsync",
                    Times.Never(),
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                );
        }

        [Fact]
        public async Task GetProductByIdAsync_WithNonExistingProduct_ThrowsException()
        {
            // Arrange
            var productId = "999";

            var mockResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent(JsonSerializer.Serialize(new { error = "Product not found" }))
            };

            _handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Get && r.RequestUri.ToString().Contains(productId)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(mockResponse);

            // Setup cache mock to simulate cache miss
            object outValue;
            _cacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out outValue))
                .Returns(false);

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                _service.GetProductByIdAsync(productId));

            Assert.Contains($"Product with ID {productId} not found", exception.Message);
        }

        [Fact]
        public async Task GetProductByIdAsync_WithEmptyProductId_ThrowsArgumentException()
        {
            // Arrange
            string productId = "";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GetProductByIdAsync(productId));
        }
    }
}