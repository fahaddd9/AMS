using Attendence_Management_System.Data;
using Attendence_Management_System.Models.Admin.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Controllers.Admin;

[Authorize(Roles = IdentitySeed.AdminRole)]
[Route("admin/analytics")]
public class AdminAnalyticsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminAnalyticsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var records = await _db.Attendances.AsNoTracking()
            .Include(a => a.Course)
                .ThenInclude(c => c.Batch)
            .ToListAsync();

        var present = records.Count(r => r.Status == AttendanceStatus.Present);
        var absent = records.Count(r => r.Status == AttendanceStatus.Absent);
        var late = records.Count(r => r.Status == AttendanceStatus.Late);

        var courseGroups = records.GroupBy(r => r.CourseId).ToList();
        var courses = new List<AdminAnalyticsViewModel.CourseSummary>();

        foreach (var g in courseGroups)
        {
            var c = g.First().Course;
            var p = g.Count(x => x.Status == AttendanceStatus.Present);
            var a = g.Count(x => x.Status == AttendanceStatus.Absent);
            var l = g.Count(x => x.Status == AttendanceStatus.Late);
            var total = g.Count();

            var score = p + (l * 0.5);
            var pct = total == 0 ? 0 : (int)Math.Round((score / total) * 100);

            courses.Add(new AdminAnalyticsViewModel.CourseSummary
            {
                CourseId = g.Key,
                Code = c.UniqueCode,
                Name = c.Name,
                BatchName = c.Batch.Name,
                Total = total,
                Present = p,
                Absent = a,
                Late = l,
                Percentage = Math.Clamp(pct, 0, 100)
            });
        }

        courses = courses.OrderByDescending(c => c.Total).ThenBy(c => c.Code).ToList();

        // user counts
        var students = await _userManager.GetUsersInRoleAsync(IdentitySeed.StudentRole);
        var teachers = await _userManager.GetUsersInRoleAsync(IdentitySeed.TeacherRole);
        var totalCourses = await _db.Courses.AsNoTracking().CountAsync();

        var vm = new AdminAnalyticsViewModel
        {
            TotalRecords = records.Count,
            Present = present,
            Absent = absent,
            Late = late,
            TotalStudents = students.Count,
            TotalTeachers = teachers.Count,
            TotalCourses = totalCourses,
            Courses = courses
        };

        return View(vm);
    }
}
