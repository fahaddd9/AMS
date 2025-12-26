using System.ComponentModel.DataAnnotations;

namespace Attendence_Management_System.Models.Admin.Users;

public class EditUserViewModel
{
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    [Display(Name = "Batch")]
    public int? BatchId { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "New Password (optional)")]
    public string? NewPassword { get; set; }
}
