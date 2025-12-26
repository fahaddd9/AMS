namespace Attendence_Management_System.Models.Student.Reports;

public class StudentCourseReportViewModel
{
    public int CourseId { get; set; }
    public string CourseDisplay { get; set; } = string.Empty;

    public int Total { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }
    public int Percentage { get; set; }

    public List<StudentCourseReportRowViewModel> Rows { get; set; } = new();
}
