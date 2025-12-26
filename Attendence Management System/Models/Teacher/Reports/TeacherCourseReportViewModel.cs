namespace Attendence_Management_System.Models.Teacher.Reports;

public class TeacherCourseReportViewModel
{
    public int CourseId { get; set; }
    public string CourseDisplay { get; set; } = string.Empty;

    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    public int TotalRecords { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }

    public List<TeacherCourseReportRowViewModel> Rows { get; set; } = new();
}
