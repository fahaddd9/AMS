using Attendence_Management_System.Data;
using Attendence_Management_System.Models.Teacher.Students;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Controllers.Teacher;

[Authorize(Roles = IdentitySeed.TeacherRole)]
[Route("teacher/students")]
public class TeacherStudentsController : Controller
{
    private readonly ApplicationDbContext _db;

    public TeacherStudentsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q = null)
    {
        var teacherId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(teacherId)) return Unauthorized();

        // teacher's course ids
        var courseIds = await _db.TeacherAssignments.AsNoTracking()
            .Where(a => a.TeacherId == teacherId)
            .Select(a => a.CourseId)
            .ToListAsync();

        if (courseIds.Count == 0)
            return View(new List<TeacherStudentListItemViewModel>());

        // students who are enrolled in any of teacher's courses
        var query = _db.Enrollments.AsNoTracking()
            .Where(e => courseIds.Contains(e.CourseId))
            .Include(e => e.Student)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(e => e.Student.Name.Contains(q) || (e.Student.Email != null && e.Student.Email.Contains(q)));
        }

        var grouped = await query
            .GroupBy(e => new { e.StudentId, e.Student.Name, Email = e.Student.Email })
            .Select(g => new TeacherStudentListItemViewModel
            {
                StudentId = g.Key.StudentId,
                StudentName = g.Key.Name,
                StudentEmail = g.Key.Email ?? "",
                EnrolledCoursesWithMe = g.Count()
            })
            .OrderByDescending(s => s.EnrolledCoursesWithMe)
            .ThenBy(s => s.StudentName)
            .ToListAsync();

        ViewBag.Query = q ?? "";
        return View(grouped);
    }
}
