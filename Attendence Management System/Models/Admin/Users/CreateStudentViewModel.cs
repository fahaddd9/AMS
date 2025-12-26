using System.ComponentModel.DataAnnotations;

namespace Attendence_Management_System.Models.Admin.Users;

public class CreateStudentViewModel
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Batch")]
    public int? BatchId { get; set; }
}
