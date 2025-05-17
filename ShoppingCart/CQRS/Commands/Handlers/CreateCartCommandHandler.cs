using MediatR;
using ShoppingCart.Models;
using ShoppingCart.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace ShoppingCart.CQRS.Commands.Handlers
{
    public class CreateCartCommandHandler : IRequestHandler<CreateCartCommand, string>
    {
        private readonly CartRepository _repo;
        public CreateCartCommandHandler(CartRepository repo)
        {
            _repo = repo;
        }
        public async Task<string> Handle(CreateCartCommand request, CancellationToken cancelationToken)
        {
            var cart = new Cart
            {
                Id = Guid.NewGuid().ToString(),
                UserId = request.UserId
            };
            await _repo.CreateAsync(cart);
            return cart.Id;
        }
    }
}