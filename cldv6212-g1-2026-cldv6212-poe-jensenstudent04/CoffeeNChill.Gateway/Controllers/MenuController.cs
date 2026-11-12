using CoffeeNChill.Gateway.Dtos;
using CoffeeNChill.Gateway.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChill.Gateway.Controllers;

/// <summary>The café menu. Reading is public; changing it needs the Admin or Staff role.</summary>
[ApiController]
[Route("api/menu")]
[Produces("application/json")]
public class MenuController(FunctionsClient functions) : ControllerBase
{
    /// <summary>List every menu item.</summary>
    /// <response code="200">The full menu.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<MenuItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<MenuItemDto>>> GetAll(CancellationToken ct)
        => Ok(await functions.GetMenuAsync(ct));

    /// <summary>List the menu items in one category.</summary>
    /// <param name="category">Category name, e.g. Coffee.</param>
    /// <response code="200">Items in that category (empty list if there are none).</response>
    [HttpGet("category/{category}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<MenuItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<List<MenuItemDto>>> GetByCategory(string category, CancellationToken ct)
        => Ok(await functions.GetMenuByCategoryAsync(category, ct));

    /// <summary>Add a menu item (Admin or Staff).</summary>
    /// <response code="201">Item created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is a Customer.</response>
    /// <response code="409">An item with this category and SKU already exists.</response>
    [HttpPost]
    [Authorize(Roles = Roles.AdminOrStaff)]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MenuItemDto>> Create(CreateMenuItemRequest request, CancellationToken ct)
    {
        var created = await functions.CreateMenuItemAsync(request, ct);
        return CreatedAtAction(nameof(GetByCategory), new { category = created.Category }, created);
    }

    /// <summary>Change a menu item (Admin or Staff).</summary>
    /// <param name="category">Category of the item.</param>
    /// <param name="sku">SKU of the item.</param>
    /// <response code="200">Item updated.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is a Customer.</response>
    /// <response code="404">No such item.</response>
    [HttpPut("{category}/{sku}")]
    [Authorize(Roles = Roles.AdminOrStaff)]
    [ProducesResponseType(typeof(MenuItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MenuItemDto>> Update(string category, string sku, UpdateMenuItemRequest request, CancellationToken ct)
        => Ok(await functions.UpdateMenuItemAsync(category, sku, request, ct));

    /// <summary>Remove a menu item (Admin or Staff).</summary>
    /// <param name="category">Category of the item.</param>
    /// <param name="sku">SKU of the item.</param>
    /// <response code="204">Item deleted.</response>
    /// <response code="401">Missing or invalid token.</response>
    /// <response code="403">The caller is a Customer.</response>
    /// <response code="404">No such item.</response>
    [HttpDelete("{category}/{sku}")]
    [Authorize(Roles = Roles.AdminOrStaff)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string category, string sku, CancellationToken ct)
    {
        await functions.DeleteMenuItemAsync(category, sku, ct);
        return NoContent();
    }
}
