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

        public ProductService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ProductDto> GetProductByIdAsync(string productId)
        {
            var response = await _httpClient.GetAsync($"http://localhost:4000/products/{productId}");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Product with ID {productId} not found. Status code: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var product = JsonSerializer.Deserialize<ProductDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return product;
        }
    }
}
