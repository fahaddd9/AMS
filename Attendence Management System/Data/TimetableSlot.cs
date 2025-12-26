namespace Attendence_Management_System.Data;

public class TimetableSlot
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;

    public DayOfWeek DayOfWeek { get; set; }

    // optional textual time range (e.g., "09:00-10:30")
    public string? TimeRange { get; set; }
}
