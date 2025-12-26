namespace Attendence_Management_System.Models.Student.Enrollment;

public class AvailableCourseViewModel
{
    public int CourseId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public string BatchName { get; set; } = string.Empty;

    public bool IsEnrolled { get; set; }
}
