using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class Customer
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? ContactInfo { get; set; }

    public DateTime? LastCheckedAt { get; set; }

    public User? User { get; set; }
    public ICollection<Request> Requests { get; set; } = new List<Request>();
}