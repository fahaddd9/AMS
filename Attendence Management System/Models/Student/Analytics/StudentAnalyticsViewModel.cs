namespace Attendence_Management_System.Models.Student.Analytics;

public class StudentAnalyticsViewModel
{
    public int TotalRecords { get; set; }
    public int Present { get; set; }
    public int Absent { get; set; }
    public int Late { get; set; }

    public List<CourseSummary> Courses { get; set; } = new();

    public class CourseSummary
    {
        public int CourseId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Present { get; set; }
        public int Absent { get; set; }
        public int Late { get; set; }
        public int Percentage { get; set; }
    }
}
