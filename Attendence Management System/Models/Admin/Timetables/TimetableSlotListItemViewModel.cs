namespace Attendence_Management_System.Models.Admin.Timetables;

public class TimetableSlotListItemViewModel
{
    public int Id { get; set; }

    public int CourseId { get; set; }
    public string CourseDisplay { get; set; } = string.Empty;

    public DayOfWeek DayOfWeek { get; set; }
    public string? TimeRange { get; set; }
}
