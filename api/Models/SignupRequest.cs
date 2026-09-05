using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class SignupRequest
{
    [Required]
    public string? Name { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [MinLength(8)]
    public string? Password { get; set; }

    public string? Role { get; set; }
}
