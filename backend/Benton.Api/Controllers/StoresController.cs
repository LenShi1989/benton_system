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
public class StoresController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public StoresController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StoreDto>>> GetStores([FromQuery] bool onlyActive = false)
    {
        var query = _db.Stores.AsQueryable();
        if (onlyActive)
        {
            query = query.Where(s => s.IsActive);
        }

        var stores = await query.OrderByDescending(s => s.Id)
            .Select(s => new StoreDto(s.Id, s.Name, s.Phone, s.Address, s.IsActive, s.CreatedAt))
            .ToListAsync();

        return Ok(stores);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StoreDto>> GetStore(int id)
    {
        var store = await _db.Stores.FindAsync(id);
        if (store == null) return NotFound();

        return Ok(new StoreDto(store.Id, store.Name, store.Phone, store.Address, store.IsActive, store.CreatedAt));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<StoreDto>> CreateStore([FromBody] CreateStoreDto dto)
    {
        var store = new Store
        {
            Name = dto.Name,
            Phone = dto.Phone,
            Address = dto.Address,
            IsActive = dto.IsActive
        };

        _db.Stores.Add(store);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetStore), new { id = store.Id },
            new StoreDto(store.Id, store.Name, store.Phone, store.Address, store.IsActive, store.CreatedAt));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStore(int id, [FromBody] CreateStoreDto dto)
    {
        var store = await _db.Stores.FindAsync(id);
        if (store == null) return NotFound();

        store.Name = dto.Name;
        store.Phone = dto.Phone;
        store.Address = dto.Address;
        store.IsActive = dto.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteStore(int id)
    {
        var store = await _db.Stores.FindAsync(id);
        if (store == null) return NotFound();

        _db.Stores.Remove(store);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
