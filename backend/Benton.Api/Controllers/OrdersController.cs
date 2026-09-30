using System.Security.Claims;
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
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public OrdersController(ApplicationDbContext db)
    {
        _db = db;
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<IEnumerable<OrderSessionDto>>> GetSessions()
    {
        var sessions = await _db.OrderSessions
            .Include(s => s.Store)
            .Include(s => s.CreatedByUser)
            .Include(s => s.OrderItems)
            .OrderByDescending(s => s.Id)
            .Select(s => new OrderSessionDto(
                s.Id,
                s.Title,
                s.StoreId,
                s.Store != null ? s.Store.Name : "",
                s.Status,
                s.Deadline,
                s.CreatedByUserId,
                s.CreatedByUser != null ? s.CreatedByUser.FullName : "",
                s.CreatedAt,
                s.OrderItems.Sum(i => i.Quantity),
                s.OrderItems.Sum(i => i.Quantity * i.UnitPrice)
            ))
            .ToListAsync();

        return Ok(sessions);
    }

    [HttpGet("sessions/{id}")]
    public async Task<ActionResult<OrderSessionDto>> GetSession(int id)
    {
        var session = await _db.OrderSessions
            .Include(s => s.Store)
            .Include(s => s.CreatedByUser)
            .Include(s => s.OrderItems)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return NotFound();

        var dto = new OrderSessionDto(
            session.Id,
            session.Title,
            session.StoreId,
            session.Store != null ? session.Store.Name : "",
            session.Status,
            session.Deadline,
            session.CreatedByUserId,
            session.CreatedByUser != null ? session.CreatedByUser.FullName : "",
            session.CreatedAt,
            session.OrderItems.Sum(i => i.Quantity),
            session.OrderItems.Sum(i => i.Quantity * i.UnitPrice)
        );

        return Ok(dto);
    }

    [HttpPost("sessions")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<OrderSessionDto>> CreateSession([FromBody] CreateOrderSessionDto dto)
    {
        var store = await _db.Stores.FindAsync(dto.StoreId);
        if (store == null) return BadRequest("指定店家不存在");

        var userId = GetCurrentUserId();
        var session = new OrderSession
        {
            Title = dto.Title,
            StoreId = dto.StoreId,
            Deadline = dto.Deadline,
            CreatedByUserId = userId,
            Status = "Open"
        };

        _db.OrderSessions.Add(session);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSession), new { id = session.Id },
            new OrderSessionDto(session.Id, session.Title, session.StoreId, store.Name, session.Status, session.Deadline, session.CreatedByUserId, User.Identity?.Name ?? "", session.CreatedAt, 0, 0));
    }

    [HttpPut("sessions/{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateSessionStatus(int id, [FromBody] string status)
    {
        var session = await _db.OrderSessions.FindAsync(id);
        if (session == null) return NotFound();

        session.Status = status; // "Open", "Closed", "Canceled"
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("sessions/{sessionId}/items")]
    public async Task<ActionResult<IEnumerable<OrderItemDto>>> GetOrderItems(int sessionId)
    {
        var items = await _db.OrderItems
            .Where(i => i.OrderSessionId == sessionId)
            .Include(i => i.User)
            .Include(i => i.MenuItem)
            .OrderBy(i => i.Id)
            .Select(i => new OrderItemDto(
                i.Id,
                i.OrderSessionId,
                i.UserId,
                i.User != null ? i.User.FullName : "",
                i.MenuItemId,
                i.MenuItem != null ? i.MenuItem.Name : "",
                i.Quantity,
                i.UnitPrice,
                i.Quantity * i.UnitPrice,
                i.Note
            ))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("items")]
    public async Task<ActionResult<OrderItemDto>> AddOrderItem([FromBody] CreateOrderItemDto dto)
    {
        var session = await _db.OrderSessions.FindAsync(dto.OrderSessionId);
        if (session == null || session.Status != "Open")
        {
            return BadRequest("該團購場次不存在或已截止結單");
        }

        var menuItem = await _db.MenuItems.FindAsync(dto.MenuItemId);
        if (menuItem == null || menuItem.StoreId != session.StoreId)
        {
            return BadRequest("無效的菜單項目");
        }

        var userId = GetCurrentUserId();

        var item = new OrderItem
        {
            OrderSessionId = dto.OrderSessionId,
            UserId = userId,
            MenuItemId = dto.MenuItemId,
            Quantity = dto.Quantity,
            UnitPrice = menuItem.Price,
            Note = dto.Note
        };

        _db.OrderItems.Add(item);
        await _db.SaveChangesAsync();

        var user = await _db.Users.FindAsync(userId);

        return Ok(new OrderItemDto(
            item.Id,
            item.OrderSessionId,
            item.UserId,
            user?.FullName ?? "",
            item.MenuItemId,
            menuItem.Name,
            item.Quantity,
            item.UnitPrice,
            item.Quantity * item.UnitPrice,
            item.Note
        ));
    }

    [HttpDelete("items/{id}")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        var userId = GetCurrentUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        var item = await _db.OrderItems
            .Include(i => i.OrderSession)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (item == null) return NotFound();

        // Allow deletion if the user is an Admin OR the owner of the item (if session is open)
        if (userRole != "Admin" && item.UserId != userId)
        {
            return Forbid();
        }

        if (item.OrderSession?.Status != "Open" && userRole != "Admin")
        {
            return BadRequest("團購已截止，無法修改點餐明細");
        }

        _db.OrderItems.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
