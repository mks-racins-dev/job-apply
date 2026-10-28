using System.ComponentModel.DataAnnotations;

namespace JobMagnet.Web.Components.Pages;

public class LoginForm
{
    private const int MinPasswordLength = 8;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(MinPasswordLength, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;
}
