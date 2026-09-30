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
public class UsersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogService _auditLog;

    public UsersController(ApplicationDbContext db, IAuditLogService auditLog)
    {
        _db = db;
        _auditLog = auditLog;
    }

    private string GetCurrentUserName() => User.Identity?.Name ?? "Admin";

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _db.Users
            .Include(u => u.RoleEntity)
            .OrderBy(u => u.Id)
            .Select(u => new UserDto(
                u.Id,
                u.Username,
                u.FullName,
                u.RoleEntity != null ? u.RoleEntity.Name : u.Role,
                u.RoleId,
                u.CreatedAt
            ))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _db.Users.Include(u => u.RoleEntity).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        return Ok(new UserDto(
            user.Id,
            user.Username,
            user.FullName,
            user.RoleEntity != null ? user.RoleEntity.Name : user.Role,
            user.RoleId,
            user.CreatedAt
        ));
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Username == dto.Username))
        {
            return BadRequest(new { message = "該帳號名稱已存在" });
        }

        var roleEntity = dto.RoleId.HasValue ? await _db.Roles.FindAsync(dto.RoleId.Value) : null;
        var roleName = roleEntity?.Name ?? dto.Role;

        var user = new User
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(string.IsNullOrWhiteSpace(dto.Password) ? "admin123" : dto.Password),
            FullName = dto.FullName,
            Role = roleName,
            RoleId = roleEntity?.Id
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(null, GetCurrentUserName(), "新增使用者", $"帳號: {user.Username}, 姓名: {user.FullName}, 角色: {user.Role}");

        return CreatedAtAction(nameof(GetUser), new { id = user.Id },
            new UserDto(user.Id, user.Username, user.FullName, user.Role, user.RoleId, user.CreatedAt));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        var roleEntity = dto.RoleId.HasValue ? await _db.Roles.FindAsync(dto.RoleId.Value) : null;
        var roleName = roleEntity?.Name ?? dto.Role;

        user.FullName = dto.FullName;
        user.Role = roleName;
        user.RoleId = roleEntity?.Id;

        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(null, GetCurrentUserName(), "修改使用者資料", $"帳號: {user.Username}, 新姓名: {user.FullName}, 新角色: {user.Role}");

        return NoContent();
    }

    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(null, GetCurrentUserName(), "重設使用者密碼", $"帳號: {user.Username} 密碼已重置為預設密碼 admin123");

        return Ok(new { message = "密碼已成功重置為預設密碼 admin123" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (user.Username.ToLower() == "admin")
        {
            return BadRequest(new { message = "無法刪除預設系統管理員帳號 (admin)" });
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        await _auditLog.LogAsync(null, GetCurrentUserName(), "刪除使用者", $"已刪除使用者帳號: {user.Username}");

        return NoContent();
    }
}
