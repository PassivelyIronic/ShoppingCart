using Microsoft.Extensions.Caching.Memory;
using ShoppingCart.Models;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShoppingCart.Services
{
    public class ProductService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        public ProductService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;
        }
        //cache

        public async Task<ProductDto> GetProductByIdAsync(string productId)
        {
            if (string.IsNullOrEmpty(productId))
                throw new ArgumentException("Product ID cannot be null or empty", nameof(productId));

            string cacheKey = $"product_{productId}";

            if (_cache.TryGetValue(cacheKey, out ProductDto cachedProduct))
                return cachedProduct;

            try
            {
                var response = await _httpClient.GetAsync($"http://localhost:4000/products/{productId}");

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        throw new Exception($"Product with ID {productId} not found.");
                    else
                        throw new Exception($"Failed to retrieve product. Status code: {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var product = JsonSerializer.Deserialize<ProductDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (product == null)
                    throw new Exception($"Failed to deserialize product with ID {productId}");

                _cache.Set(cacheKey, product, _cacheDuration);

                return product;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Communication error with Product service: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new Exception($"Error parsing product data: {ex.Message}", ex);
            }
        }
    }
}