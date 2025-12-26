using Attendence_Management_System.Data;

namespace Attendence_Management_System.Models.Student.Reports;

public class StudentCourseReportRowViewModel
{
    public DateOnly Date { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public bool IsMakeUpLecture { get; set; }
    public AttendanceStatus Status { get; set; }
}
