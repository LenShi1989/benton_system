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
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IJwtService _jwtService;
    private readonly IAuditLogService _auditLog;

    public AuthController(ApplicationDbContext db, IJwtService jwtService, IAuditLogService auditLog)
    {
        _db = db;
        _jwtService = jwtService;
        _auditLog = auditLog;
    }

    private List<string> ParseAllowedMenus(string? allowedMenusJson, string roleName)
    {
        if (!string.IsNullOrWhiteSpace(allowedMenusJson))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<string>>(allowedMenusJson);
                if (list != null && list.Count > 0) return list;
            }
            catch { }
        }

        if (roleName == "Admin")
        {
            return new List<string> { "/", "/orders", "/stores", "/users", "/roles", "/logs" };
        }
        return new List<string> { "/", "/orders" };
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        var user = await _db.Users.Include(u => u.RoleEntity).FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            await _auditLog.LogAsync(null, dto.Username, "登入失敗", "帳號或密碼錯誤", HttpContext.Connection.RemoteIpAddress?.ToString());
            return Unauthorized(new { message = "帳號或密碼錯誤" });
        }

        var allowedMenus = ParseAllowedMenus(user.RoleEntity?.AllowedMenus, user.Role);
        var token = _jwtService.GenerateToken(user);

        await _auditLog.LogAsync(user.Id, user.FullName, "使用者登入", $"角色: {user.Role}", HttpContext.Connection.RemoteIpAddress?.ToString());

        return Ok(new AuthResponseDto(token, user.Username, user.FullName, user.Role, allowedMenus));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthResponseDto>> GetMe()
    {
        var username = User.Identity?.Name;
        var user = await _db.Users.Include(u => u.RoleEntity).FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return NotFound();

        var allowedMenus = ParseAllowedMenus(user.RoleEntity?.AllowedMenus, user.Role);
        var token = _jwtService.GenerateToken(user);

        return Ok(new AuthResponseDto(token, user.Username, user.FullName, user.Role, allowedMenus));
    }
}
