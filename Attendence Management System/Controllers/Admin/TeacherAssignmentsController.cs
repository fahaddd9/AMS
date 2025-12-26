using Attendence_Management_System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Controllers.Admin;

[Authorize(Roles = IdentitySeed.AdminRole)]
[Route("admin/assignments")]
public class TeacherAssignmentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public TeacherAssignmentsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? courseId = null)
    {
        await PopulateCoursesAsync(courseId);
        await PopulateTeachersAsync();

        var query = _db.TeacherAssignments.AsNoTracking()
            .Include(a => a.Course)
            .ThenInclude(c => c.Batch)
            .Include(a => a.Teacher)
            .AsQueryable();

        if (courseId.HasValue)
            query = query.Where(a => a.CourseId == courseId.Value);

        var items = await query
            .OrderBy(a => a.Course.UniqueCode)
            .ThenBy(a => a.Teacher.Name)
            .ToListAsync();

        return View(items);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int courseId, string teacherId)
    {
        var courseExists = await _db.Courses.AnyAsync(c => c.Id == courseId);
        if (!courseExists)
        {
            TempData["Error"] = "Invalid course.";
            return RedirectToAction(nameof(Index));
        }

        var teacher = await _db.Users.FirstOrDefaultAsync(u => u.Id == teacherId);
        if (teacher == null)
        {
            TempData["Error"] = "Invalid teacher.";
            return RedirectToAction(nameof(Index), new { courseId });
        }

        if (!await _userManager.IsInRoleAsync(teacher, IdentitySeed.TeacherRole))
        {
            TempData["Error"] = "Selected user is not a teacher.";
            return RedirectToAction(nameof(Index), new { courseId });
        }

        var exists = await _db.TeacherAssignments.AnyAsync(a => a.CourseId == courseId && a.TeacherId == teacherId);
        if (exists)
        {
            TempData["Error"] = "This teacher is already assigned to that course.";
            return RedirectToAction(nameof(Index), new { courseId });
        }

        _db.TeacherAssignments.Add(new TeacherAssignment { CourseId = courseId, TeacherId = teacherId });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Teacher assigned to course.";
        return RedirectToAction(nameof(Index), new { courseId });
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? courseId = null)
    {
        var assignment = await _db.TeacherAssignments.FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();

        _db.TeacherAssignments.Remove(assignment);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Assignment removed.";
        return RedirectToAction(nameof(Index), new { courseId });
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

    private async Task PopulateTeachersAsync()
    {
        var teachers = await _userManager.GetUsersInRoleAsync(IdentitySeed.TeacherRole);
        ViewBag.Teachers = teachers
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem($"{t.Name} ({t.Email})", t.Id))
            .ToList();
    }
}
