using System.ComponentModel.DataAnnotations;
using Attendence_Management_System.Data;

namespace Attendence_Management_System.Models.Teacher.Attendance;

public class AttendanceMarkRowViewModel
{
    [Required]
    public string StudentId { get; set; } = string.Empty;

    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;

    [Required]
    public AttendanceStatus Status { get; set; }
}
