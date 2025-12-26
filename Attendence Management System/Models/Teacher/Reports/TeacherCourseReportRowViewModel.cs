using Attendence_Management_System.Data;

namespace Attendence_Management_System.Models.Teacher.Reports;

public class TeacherCourseReportRowViewModel
{
    public DateOnly Date { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; }
    public bool IsMakeUpLecture { get; set; }
}
