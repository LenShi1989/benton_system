using System.Text.Json;
using Benton.Api.Data;
using Benton.Api.DTOs;
using Benton.Api.Models;
using Benton.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Benton.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _auditLog;

    public RolesController(ApplicationDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    private string GetCurrentUserName() => User.Identity?.Name ?? "Admin";

    private List<string> ParseMenus(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
    {
        var roles = await _db.Roles.OrderBy(r => r.Id).ToListAsync();
        var dtos = roles.Select(r => new RoleDto(
            r.Id,
            r.Name,
            r.Description,
            ParseMenus(r.AllowedMenus),
            r.CreatedAt
        ));

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RoleDto>> GetRole(int id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound();

        return Ok(new RoleDto(role.Id, role.Name, role.Description, ParseMenus(role.AllowedMenus), role.CreatedAt));
    }

    [HttpPost]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleDto dto)
    {
        if (await _db.Roles.AnyAsync(r => r.Name == dto.Name))
        {
            return BadRequest(new { message = "角色名稱已存在" });
        }

        var jsonMenus = JsonSerializer.Serialize(dto.AllowedMenus);
        var role = new Role
        {
            Name = dto.Name,
            Description = dto.Description,
            AllowedMenus = jsonMenus
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(null, GetCurrentUserName(), "新增角色", $"角色名稱: {role.Name}, 側邊欄權限: {jsonMenus}");

        return CreatedAtAction(nameof(GetRole), new { id = role.Id },
            new RoleDto(role.Id, role.Name, role.Description, dto.AllowedMenus, role.CreatedAt));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] CreateRoleDto dto)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound();

        var jsonMenus = JsonSerializer.Serialize(dto.AllowedMenus);
        role.Name = dto.Name;
        role.Description = dto.Description;
        role.AllowedMenus = jsonMenus;

        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(null, GetCurrentUserName(), "修改角色權限", $"角色: {role.Name}, 新側邊欄權限: {jsonMenus}");

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRole(int id)
    {
        var role = await _db.Roles.FindAsync(id);
        if (role == null) return NotFound();

        if (role.Name == "Admin" || role.Name == "User")
        {
            return BadRequest(new { message = "無法刪除預設角色 (Admin / User)" });
        }

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(null, GetCurrentUserName(), "刪除角色", $"已刪除角色: {role.Name}");

        return NoContent();
    }
}
