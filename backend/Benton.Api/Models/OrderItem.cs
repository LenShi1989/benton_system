using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Benton.Api.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderSessionId { get; set; }

    public int UserId { get; set; }

    public int MenuItemId { get; set; }

    public int Quantity { get; set; } = 1;

    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    [MaxLength(255)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public OrderSession? OrderSession { get; set; }
    public User? User { get; set; }
    public MenuItem? MenuItem { get; set; }
}
