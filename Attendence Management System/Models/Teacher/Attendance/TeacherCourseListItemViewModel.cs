namespace Attendence_Management_System.Models.Teacher.Attendance;

public class TeacherCourseListItemViewModel
{
    public int CourseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BatchName { get; set; } = string.Empty;

    public int EnrolledStudents { get; set; }
    public int TimetableDaysCount { get; set; }
}
