namespace ShoppingCart.Events
{
    public class ProductRemovedFromCartEvent : CartEvent
    {
        public string ProductId { get; set; }
    }
}