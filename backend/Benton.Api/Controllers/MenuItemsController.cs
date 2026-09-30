using Benton.Api.Data;
using Benton.Api.DTOs;
using Benton.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Benton.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MenuItemsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public MenuItemsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("store/{storeId}")]
    public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetMenuItemsByStore(int storeId, [FromQuery] bool onlyActive = false)
    {
        var query = _db.MenuItems.Where(m => m.StoreId == storeId);
        if (onlyActive)
        {
            query = query.Where(m => m.IsActive);
        }

        var items = await query.OrderBy(m => m.Id)
            .Select(m => new MenuItemDto(m.Id, m.StoreId, m.Name, m.Description, m.Price, m.IsActive))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<MenuItemDto>> CreateMenuItem([FromBody] CreateMenuItemDto dto)
    {
        var storeExists = await _db.Stores.AnyAsync(s => s.Id == dto.StoreId);
        if (!storeExists) return BadRequest("指定店家不存在");

        var item = new MenuItem
        {
            StoreId = dto.StoreId,
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            IsActive = dto.IsActive
        };

        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync();

        return Ok(new MenuItemDto(item.Id, item.StoreId, item.Name, item.Description, item.Price, item.IsActive));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateMenuItem(int id, [FromBody] CreateMenuItemDto dto)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item == null) return NotFound();

        item.Name = dto.Name;
        item.Description = dto.Description;
        item.Price = dto.Price;
        item.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteMenuItem(int id)
    {
        var item = await _db.MenuItems.FindAsync(id);
        if (item == null) return NotFound();

        _db.MenuItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
