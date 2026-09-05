using System.ComponentModel.DataAnnotations;

namespace api.Models;

public enum UserRole
{
    Admin = 0,
    Craftsman = 1,
    Customer = 2
}

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(254)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }
}
