using System.ComponentModel.DataAnnotations;

namespace Attendence_Management_System.Models.Admin.Courses;

public class CourseFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(30)]
    [Display(Name = "Course Code")]
    public string UniqueCode { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    [Display(Name = "Course Name")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 30)]
    [Display(Name = "Credit Hours")]
    public int CreditHours { get; set; }

    [Required]
    [Display(Name = "Batch")]
    public int? BatchId { get; set; }
}
