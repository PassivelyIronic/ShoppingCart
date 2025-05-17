namespace ShoppingCart.Models
{
    public class CartItem
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; } // fetched from ProductService
        public string Name { get; set; } // fetched from ProductService
    }
}