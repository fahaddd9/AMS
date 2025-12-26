namespace Attendence_Management_System.Data;

public enum AttendanceStatus
{
    Present = 1,
    Absent = 2,
    Late = 3
}

public class Attendance
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = default!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = default!;

    // which teacher marked this record (you requested teacher-specific records)
    public string MarkedByTeacherId { get; set; } = string.Empty;
    public ApplicationUser MarkedByTeacher { get; set; } = default!;

    public DateOnly Date { get; set; }
    public AttendanceStatus Status { get; set; }

    public bool IsMakeUpLecture { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
