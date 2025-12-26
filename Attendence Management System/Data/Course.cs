namespace Attendence_Management_System.Data;

public class Course
{
    public int Id { get; set; }

    public string UniqueCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CreditHours { get; set; }

    public int BatchId { get; set; }
    public Batch Batch { get; set; } = default!;

    public ICollection<TimetableSlot> TimetableSlots { get; set; } = new List<TimetableSlot>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<TeacherAssignment> TeacherAssignments { get; set; } = new List<TeacherAssignment>();
}
