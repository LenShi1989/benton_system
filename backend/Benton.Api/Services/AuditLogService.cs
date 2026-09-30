using Benton.Api.Data;
using Benton.Api.Models;

namespace Benton.Api.Services;

public interface IAuditLogService
{
    Task LogAsync(int? userId, string userName, string action, string? details = null, string? ipAddress = null);
}

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _db;

    public AuditLogService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(int? userId, string userName, string action, string? details = null, string? ipAddress = null)
    {
        try
        {
            var log = new AuditLog
            {
                UserId = userId,
                UserName = string.IsNullOrWhiteSpace(userName) ? "System" : userName,
                Action = action,
                Details = details,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Audit log write error: {ex.Message}");
        }
    }
}
