using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class Craft
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ICollection<CraftsmanCraft> CraftsmanCrafts { get; set; } = new List<CraftsmanCraft>();
    public ICollection<Request> Requests { get; set; } = new List<Request>();
}
