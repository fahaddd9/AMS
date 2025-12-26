using Attendence_Management_System.Data;

namespace Attendence_Management_System.Models.Teacher.Reports;

public class TeacherStudentReportRowViewModel
{
    public DateOnly Date { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public AttendanceStatus Status { get; set; }
    public bool IsMakeUpLecture { get; set; }
}
