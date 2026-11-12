using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.Dtos;

/// <summary>A menu item as returned by the Functions app.</summary>
public class MenuItemDto
{
    /// <summary>Category (the Menu table PartitionKey), e.g. Coffee.</summary>
    public string Category { get; set; } = "";
    /// <summary>Unique item code within the category (the RowKey).</summary>
    public string Sku { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public double Price { get; set; }
    public bool IsAvailable { get; set; }
}

/// <summary>Body for <c>POST /api/menu</c>.</summary>
public class CreateMenuItemRequest
{
    /// <example>Coffee</example>
    [Required, StringLength(50)]
    public string Category { get; set; } = "";

    /// <example>COF-001</example>
    [Required, StringLength(50)]
    public string Sku { get; set; } = "";

    /// <example>Flat White</example>
    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>Price in rand; must be zero or greater.</summary>
    [Range(0, 100000)]
    public double Price { get; set; }

    public bool IsAvailable { get; set; } = true;
}

/// <summary>Body for <c>PUT /api/menu/{category}/{sku}</c>. Category and SKU come from the URL.</summary>
public class UpdateMenuItemRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0, 100000)]
    public double Price { get; set; }

    public bool IsAvailable { get; set; } = true;
}
