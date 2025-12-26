namespace Attendence_Management_System.Models.Student.Dashboard;

public class StudentDashboardViewModel
{
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;

    public List<StudentCourseAttendanceViewModel> Courses { get; set; } = new();
}
