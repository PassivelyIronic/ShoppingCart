namespace ShoppingCart.Events
{
    public class ProductAddedToCartEvent : CartEvent
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ProductName { get; set; }
    }
}