namespace Attendence_Management_System.Data;

public class Enrollment
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = default!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;
}
