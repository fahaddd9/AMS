namespace Attendence_Management_System.Models.Teacher.Students;

public class TeacherStudentListItemViewModel
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;

    public int EnrolledCoursesWithMe { get; set; }
}
