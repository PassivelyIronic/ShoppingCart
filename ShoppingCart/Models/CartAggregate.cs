using ShoppingCart.Events;
using System.Collections.Generic;
using System.Linq;

namespace ShoppingCart.Models
{
    public class CartAggregate
    {
        public string Id { get; private set; }
        public string UserId { get; private set; }
        public List<CartItem> Items { get; private set; } = new();
        public bool IsCheckedOut { get; private set; }
        public long Version { get; private set; }
        private List<CartEvent> _uncommittedEvents = new();

        public CartAggregate() { }

        public CartAggregate(string id, string userId)
        {
            var evt = new CartCreatedEvent
            {
                CartId = id,
                UserId = userId
            };

            Apply(evt);
            _uncommittedEvents.Add(evt);
        }

        public static CartAggregate FromEvents(List<CartEvent> events)
        {
            if (!events.Any()) return null;

            var aggregate = new CartAggregate();
            foreach (var evt in events.OrderBy(e => e.SequenceNumber))
            {
                aggregate.Apply(evt);
            }

            aggregate._uncommittedEvents.Clear();
            return aggregate;
        }

        public void AddProduct(string productId, int quantity, decimal price, string productName)
        {
            if (IsCheckedOut)
                throw new InvalidOperationException("Cannot modify checked out cart");

            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero");

            var evt = new ProductAddedToCartEvent
            {
                CartId = Id,
                UserId = UserId,
                ProductId = productId,
                Quantity = quantity,
                Price = price,
                ProductName = productName
            };

            Apply(evt);
            _uncommittedEvents.Add(evt);
        }

        public void RemoveProduct(string productId)
        {
            if (IsCheckedOut)
                throw new InvalidOperationException("Cannot modify checked out cart");

            if (string.IsNullOrEmpty(productId))
                throw new ArgumentException("Product ID cannot be empty");

            var existingItem = Items.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem == null)
                throw new InvalidOperationException($"Product {productId} not found in cart");

            var evt = new ProductRemovedFromCartEvent
            {
                CartId = Id,
                UserId = UserId,
                ProductId = productId
            };

            Apply(evt);
            _uncommittedEvents.Add(evt);
        }

        public void Checkout()
        {
            if (IsCheckedOut)
                throw new InvalidOperationException("Cart is already checked out");

            if (!Items.Any())
                throw new InvalidOperationException("Cannot checkout empty cart");

            var totalValue = TotalValue;
            var totalItems = Items.Sum(i => i.Quantity);

            var evt = new CartCheckedOutEvent
            {
                CartId = Id,
                UserId = UserId,
                TotalValue = totalValue,
                TotalItems = totalItems
            };

            Apply(evt);
            _uncommittedEvents.Add(evt);
        }

        private void Apply(CartEvent evt)
        {
            switch (evt)
            {
                case CartCreatedEvent created:
                    Id = created.CartId;
                    UserId = created.UserId;
                    break;

                case ProductAddedToCartEvent added:
                    var existingItemIndex = Items.FindIndex(i => i.ProductId == added.ProductId);
                    if (existingItemIndex >= 0)
                    {
                        var existingItem = Items[existingItemIndex];
                        Items[existingItemIndex] = new CartItem
                        {
                            ProductId = existingItem.ProductId,
                            Quantity = existingItem.Quantity + added.Quantity,
                            Price = added.Price,
                            Name = added.ProductName
                        };
                    }
                    else
                    {
                        Items.Add(new CartItem
                        {
                            ProductId = added.ProductId,
                            Quantity = added.Quantity,
                            Price = added.Price,
                            Name = added.ProductName
                        });
                    }
                    break;

                case ProductRemovedFromCartEvent removed:
                    Items.RemoveAll(i => i.ProductId == removed.ProductId);
                    break;

                case CartCheckedOutEvent checkedOut:
                    IsCheckedOut = true;
                    break;
            }

            Version = evt.SequenceNumber > 0 ? evt.SequenceNumber : Version + 1;
        }

        public List<CartEvent> GetUncommittedEvents()
        {
            return new List<CartEvent>(_uncommittedEvents);
        }

        public void MarkEventsAsCommitted()
        {
            _uncommittedEvents.Clear();
        }

        public decimal TotalValue => Items.Sum(i => i.Price * i.Quantity);
        public bool HasItems => Items.Any();
        public int TotalItemsCount => Items.Sum(i => i.Quantity);
    }
}