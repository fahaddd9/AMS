using System.ComponentModel.DataAnnotations;

namespace Attendence_Management_System.Models.Admin.Batches;

public class BatchFormViewModel
{
    public int? Id { get; set; }

    [Required]
    [StringLength(50, MinimumLength = 4)]
    [Display(Name = "Batch Name")]
    public string Name { get; set; } = string.Empty;
}
