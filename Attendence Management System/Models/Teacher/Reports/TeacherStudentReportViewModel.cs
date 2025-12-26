namespace Attendence_Management_System.Models.Teacher.Reports;

public class TeacherStudentReportViewModel
{
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;

    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    public int TotalRecords { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }

    public List<TeacherStudentReportRowViewModel> Rows { get; set; } = new();
}
