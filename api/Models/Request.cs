using System.ComponentModel.DataAnnotations;

namespace api.Models;

public enum RequestStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Withdrawn = 3,
    Completed = 4
}

public class Request
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [Required]
    public int CraftsmanId { get; set; }

    [Required]
    public int CraftId { get; set; }

    [Required]
    public DateTime NeededOn { get; set; }

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Price { get; set; }

    [Required]
    public string Description { get; set; } = string.Empty;

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public Customer Customer { get; set; } = null!;
    public Craftsman Craftsman { get; set; } = null!;
    public Craft Craft { get; set; } = null!;
}