using System.ComponentModel.DataAnnotations;

namespace Benton.Api.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "User"; // "Admin", "User", "Manager", etc.

    public int? RoleId { get; set; }
    public Role? RoleEntity { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<OrderSession> CreatedSessions { get; set; } = new List<OrderSession>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
