﻿using MediatR;
using ShoppingCart.Models;
using ShoppingCart.Repositories;
using System.Threading;
using System.Threading.Tasks;

namespace ShoppingCart.CQRS.Commands.Handlers
{
    public class CreateCartCommandHandler : IRequestHandler<CreateCartCommand, string>
    {
        private readonly CartAggregateRepository _repository;

        public CreateCartCommandHandler(CartAggregateRepository repository)
        {
            _repository = repository;
        }

        public async Task<string> Handle(CreateCartCommand request, CancellationToken cancellationToken)
        {
            var existingCart = await _repository.GetByUserIdAsync(request.UserId);
            if (existingCart != null && !existingCart.IsCheckedOut)
            {
                return existingCart.Id;
            }

            var cartId = Guid.NewGuid().ToString();
            var aggregate = new CartAggregate(cartId, request.UserId);

            await _repository.SaveAsync(aggregate);

            return cartId;
        }
    }
}