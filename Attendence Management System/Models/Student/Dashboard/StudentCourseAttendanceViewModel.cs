namespace Attendence_Management_System.Models.Student.Dashboard;

public class StudentCourseAttendanceViewModel
{
    public int CourseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public int TotalMarkedLectures { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }

    public int AttendancePercentage { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
}
