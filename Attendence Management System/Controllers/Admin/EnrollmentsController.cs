using Attendence_Management_System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Controllers.Admin;

[Authorize(Roles = IdentitySeed.AdminRole)]
[Route("admin/enrollments")]
public class EnrollmentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public EnrollmentsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? courseId = null)
    {
        await PopulateCoursesAsync(courseId);
        await PopulateStudentsAsync();

        var query = _db.Enrollments.AsNoTracking()
            .Include(e => e.Course)
                .ThenInclude(c => c.Batch)
            .Include(e => e.Student)
            .AsQueryable();

        if (courseId.HasValue)
            query = query.Where(e => e.CourseId == courseId.Value);

        var items = await query
            .OrderBy(e => e.Course.UniqueCode)
            .ThenBy(e => e.Student.Name)
            .ToListAsync();

        return View(items);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int courseId, string studentId)
    {
        var courseExists = await _db.Courses.AnyAsync(c => c.Id == courseId);
        if (!courseExists)
        {
            TempData["Error"] = "Invalid course.";
            return RedirectToAction(nameof(Index));
        }

        var student = await _db.Users.FirstOrDefaultAsync(u => u.Id == studentId);
        if (student == null)
        {
            TempData["Error"] = "Invalid student.";
            return RedirectToAction(nameof(Index), new { courseId });
        }

        if (!await _userManager.IsInRoleAsync(student, IdentitySeed.StudentRole))
        {
            TempData["Error"] = "Selected user is not a student.";
            return RedirectToAction(nameof(Index), new { courseId });
        }

        var exists = await _db.Enrollments.AnyAsync(e => e.CourseId == courseId && e.StudentId == studentId);
        if (exists)
        {
            TempData["Error"] = "This student is already enrolled in that course.";
            return RedirectToAction(nameof(Index), new { courseId });
        }

        _db.Enrollments.Add(new Enrollment { CourseId = courseId, StudentId = studentId });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Student enrolled.";
        return RedirectToAction(nameof(Index), new { courseId });
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? courseId = null)
    {
        var enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.Id == id);
        if (enrollment == null) return NotFound();

        var hasAttendance = await _db.Attendances.AnyAsync(a => a.StudentId == enrollment.StudentId && a.CourseId == enrollment.CourseId);
        if (hasAttendance)
        {
            TempData["Error"] = "Cannot remove enrollment because attendance exists for this student in this course.";
            return RedirectToAction(nameof(Index), new { courseId = courseId ?? enrollment.CourseId });
        }

        _db.Enrollments.Remove(enrollment);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Enrollment removed.";
        return RedirectToAction(nameof(Index), new { courseId = courseId ?? enrollment.CourseId });
    }

    private async Task PopulateCoursesAsync(int? selected)
    {
        var courses = await _db.Courses.AsNoTracking().Include(c => c.Batch)
            .OrderBy(c => c.UniqueCode)
            .ToListAsync();

        ViewBag.Courses = courses
            .Select(c => new SelectListItem($"{c.UniqueCode} - {c.Name} ({c.Batch.Name})", c.Id.ToString(), c.Id == selected))
            .ToList();
    }

    private async Task PopulateStudentsAsync()
    {
        var students = await _userManager.GetUsersInRoleAsync(IdentitySeed.StudentRole);
        ViewBag.Students = students
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem($"{s.Name} ({s.Email})", s.Id))
            .ToList();
    }
}
