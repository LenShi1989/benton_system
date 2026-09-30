using System.ComponentModel.DataAnnotations;

namespace Benton.Api.Models;

public class Role
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Description { get; set; }

    // JSON string storing allowed menu routes, e.g. '["/", "/orders", "/stores", "/users", "/roles", "/logs"]'
    [Required]
    public string AllowedMenus { get; set; } = "[\"/\"]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<User> Users { get; set; } = new List<User>();
}
