using System.ComponentModel.DataAnnotations;

namespace Attendence_Management_System.Models.Admin.Timetables;

public class TimetableSlotFormViewModel
{
    public int? Id { get; set; }

    [Required]
    public int? CourseId { get; set; }

    [Required]
    [Display(Name = "Day")]
    public DayOfWeek? DayOfWeek { get; set; }

    [StringLength(30)]
    [Display(Name = "Time Range (optional)")]
    public string? TimeRange { get; set; }
}
