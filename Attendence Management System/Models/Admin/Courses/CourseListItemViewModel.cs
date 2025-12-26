namespace Attendence_Management_System.Models.Admin.Courses;

public class CourseListItemViewModel
{
    public int Id { get; set; }
    public string UniqueCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public string BatchName { get; set; } = string.Empty;

    public int TeachersCount { get; set; }
    public int EnrollmentsCount { get; set; }
}
