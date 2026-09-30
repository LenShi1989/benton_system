using Benton.Api.Data;
using Benton.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Benton.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AuditLogsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetLogs([FromQuery] string? search = null)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l => l.UserName.Contains(search) || l.Action.Contains(search) || (l.Details != null && l.Details.Contains(search)));
        }

        var logs = await query.OrderByDescending(l => l.Id)
            .Take(100)
            .Select(l => new AuditLogDto(
                l.Id,
                l.UserId,
                l.UserName,
                l.Action,
                l.Details,
                l.IpAddress,
                l.CreatedAt
            ))
            .ToListAsync();

        return Ok(logs);
    }
}
