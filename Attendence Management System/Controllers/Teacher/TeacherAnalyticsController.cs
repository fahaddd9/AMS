using Attendence_Management_System.Data;
using Attendence_Management_System.Models.Teacher.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Controllers.Teacher;

[Authorize(Roles = IdentitySeed.TeacherRole)]
[Route("teacher/analytics")]
public class TeacherAnalyticsController : Controller
{
    private readonly ApplicationDbContext _db;

    public TeacherAnalyticsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var teacherId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(teacherId)) return Unauthorized();

        // Teacher sees analytics only for attendance they marked
        var records = await _db.Attendances.AsNoTracking()
            .Where(a => a.MarkedByTeacherId == teacherId)
            .Include(a => a.Course)
            .ToListAsync();

        var present = records.Count(r => r.Status == AttendanceStatus.Present);
        var absent = records.Count(r => r.Status == AttendanceStatus.Absent);
        var late = records.Count(r => r.Status == AttendanceStatus.Late);

        var courseGroups = records
            .GroupBy(r => r.CourseId)
            .ToList();

        var courses = new List<TeacherAnalyticsViewModel.CourseSummary>();
        foreach (var g in courseGroups)
        {
            var c = g.First().Course;
            var p = g.Count(x => x.Status == AttendanceStatus.Present);
            var a = g.Count(x => x.Status == AttendanceStatus.Absent);
            var l = g.Count(x => x.Status == AttendanceStatus.Late);
            var total = g.Count();

            var score = p + (l * 0.5);
            var pct = total == 0 ? 0 : (int)Math.Round((score / total) * 100);

            courses.Add(new TeacherAnalyticsViewModel.CourseSummary
            {
                CourseId = g.Key,
                Code = c.UniqueCode,
                Name = c.Name,
                Total = total,
                Present = p,
                Absent = a,
                Late = l,
                Percentage = Math.Clamp(pct, 0, 100)
            });
        }

        courses = courses.OrderByDescending(c => c.Total).ThenBy(c => c.Code).ToList();

        var vm = new TeacherAnalyticsViewModel
        {
            TotalRecords = records.Count,
            Present = present,
            Absent = absent,
            Late = late,
            Courses = courses
        };

        return View(vm);
    }
}
