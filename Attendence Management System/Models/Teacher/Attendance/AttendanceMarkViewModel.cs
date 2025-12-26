using System.ComponentModel.DataAnnotations;

namespace Attendence_Management_System.Models.Teacher.Attendance;

public class AttendanceMarkViewModel
{
    [Required]
    public int? CourseId { get; set; }

    [Required]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Make-up lecture")]
    public bool IsMakeUpLecture { get; set; }

    [Required]
    public List<AttendanceMarkRowViewModel> Rows { get; set; } = new();
}
