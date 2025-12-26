namespace Attendence_Management_System.Data;

public class TeacherAssignment
{
    public int Id { get; set; }

    public string TeacherId { get; set; } = string.Empty;
    public ApplicationUser Teacher { get; set; } = default!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;
}
