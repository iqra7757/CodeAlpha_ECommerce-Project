using System;
using System.Collections.Generic;

namespace CodeAlpha_EcommerceStore.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public DateTime? OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Status { get; set; }

    public string? ShippingAddress { get; set; }

    public string CustomerName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? Email { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public string? City { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual User? User { get; set; }
}
