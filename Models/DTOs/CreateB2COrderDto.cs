using System;
using System.Collections.Generic;

namespace MediGuard.Models.DTOs
{
    public class CreateB2COrderDto
    {
        public List<CartItemDto> Items { get; set; } = new();
        public string? ShippingAddress { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class CartItemDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }
}
