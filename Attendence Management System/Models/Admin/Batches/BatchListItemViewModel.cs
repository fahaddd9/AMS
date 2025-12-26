namespace Attendence_Management_System.Models.Admin.Batches;

public class BatchListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int StudentsCount { get; set; }
    public int CoursesCount { get; set; }
}
