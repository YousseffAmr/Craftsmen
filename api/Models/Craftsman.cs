using System.ComponentModel.DataAnnotations;

namespace api.Models;

public enum CraftsmanStatus
{
    Pending = 0,
    Approved = 1,
    Refused = 2
}

public class Craftsman
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? ContactInfo { get; set; }

    public decimal DailyRate { get; set; }

    [Required]
    public CraftsmanStatus Status { get; set; } = CraftsmanStatus.Pending;

    public DateTime? LastCheckedAt { get; set; }

    public User? User { get; set; }
    public ICollection<CraftsmanCraft> CraftsmanCrafts { get; set; } = new List<CraftsmanCraft>();
    public ICollection<Request> Requests { get; set; } = new List<Request>();
}