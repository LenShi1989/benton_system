using System.ComponentModel.DataAnnotations;

namespace Benton.Api.Models;

public class OrderSession
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    public int StoreId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "Open"; // "Open", "Closed", "Canceled"

    public DateTime? Deadline { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Store? Store { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
