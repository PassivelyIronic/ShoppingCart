namespace ShoppingCart.Events
{
    public class CartCheckedOutEvent : CartEvent
    {
        public decimal TotalValue { get; set; }
        public int TotalItems { get; set; }
    }
}