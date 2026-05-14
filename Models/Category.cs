using System;
using System.Collections.Generic;

namespace CodeAlpha_EcommerceStore.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
