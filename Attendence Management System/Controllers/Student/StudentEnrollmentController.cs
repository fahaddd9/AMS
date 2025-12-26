using Attendence_Management_System.Data;
using Attendence_Management_System.Models.Student.Enrollment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Controllers.Student;

[Authorize(Roles = IdentitySeed.StudentRole)]
[Route("student/enrollment")]
public class StudentEnrollmentController : Controller
{
    private readonly ApplicationDbContext _db;

    public StudentEnrollmentController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(studentId)) return Unauthorized();

        var student = await _db.Users.AsNoTracking().Include(u => u.Batch).FirstOrDefaultAsync(u => u.Id == studentId);
        if (student == null) return Unauthorized();

        var enrolledCourseIds = await _db.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Select(e => e.CourseId)
            .ToListAsync();

        var courses = await _db.Courses.AsNoTracking()
            .Include(c => c.Batch)
            .OrderBy(c => c.UniqueCode)
            .Select(c => new AvailableCourseViewModel
            {
                CourseId = c.Id,
                Code = c.UniqueCode,
                Name = c.Name,
                CreditHours = c.CreditHours,
                BatchName = c.Batch.Name,
                IsEnrolled = enrolledCourseIds.Contains(c.Id)
            })
            .ToListAsync();

        ViewBag.StudentBatch = student.Batch?.Name ?? "-";
        return View(courses);
    }

    [HttpPost("enroll")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int courseId)
    {
        var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(studentId)) return Unauthorized();

        var course = await _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound();

        var exists = await _db.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        if (exists)
        {
            TempData["Success"] = "You are already enrolled.";
            return RedirectToAction(nameof(Index));
        }

        _db.Enrollments.Add(new Enrollment { StudentId = studentId, CourseId = courseId });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Enrolled successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("unenroll")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unenroll(int courseId)
    {
        var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(studentId)) return Unauthorized();

        var enrollment = await _db.Enrollments.FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        if (enrollment == null)
        {
            TempData["Error"] = "Enrollment not found.";
            return RedirectToAction(nameof(Index));
        }

        // do not allow unenroll if attendance already exists for this student+course
        var hasAttendance = await _db.Attendances.AnyAsync(a => a.StudentId == studentId && a.CourseId == courseId);
        if (hasAttendance)
        {
            TempData["Error"] = "You cannot unenroll because attendance has already been marked for this course.";
            return RedirectToAction(nameof(Index));
        }

        _db.Enrollments.Remove(enrollment);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Unenrolled successfully.";
        return RedirectToAction(nameof(Index));
    }
}
