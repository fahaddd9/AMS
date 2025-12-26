namespace Attendence_Management_System.Data;

public class Batch
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<ApplicationUser> Students { get; set; } = new List<ApplicationUser>();
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
