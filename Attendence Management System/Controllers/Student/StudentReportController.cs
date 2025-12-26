using Attendence_Management_System.Data;
using Attendence_Management_System.Models.Student.Reports;
using Attendence_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Controllers.Student;

[Authorize(Roles = IdentitySeed.StudentRole)]
[Route("student/report")]
public class StudentReportController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ExportService _export;

    public StudentReportController(ApplicationDbContext db, ExportService export)
    {
        _db = db;
        _export = export;
    }

    [HttpGet("{courseId:int}")]
    public async Task<IActionResult> Course(int courseId)
    {
        var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(studentId)) return Unauthorized();

        var enrolled = await _db.Enrollments.AsNoTracking().AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        if (!enrolled) return Forbid();

        var course = await _db.Courses.AsNoTracking().Include(c => c.Batch).FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound();

        // records may exist from multiple teachers; student sees all of them
        var records = await _db.Attendances.AsNoTracking()
            .Where(a => a.StudentId == studentId && a.CourseId == courseId)
            .Include(a => a.MarkedByTeacher)
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.MarkedByTeacher.Name)
            .ToListAsync();

        var present = records.Count(r => r.Status == AttendanceStatus.Present);
        var absent = records.Count(r => r.Status == AttendanceStatus.Absent);
        var late = records.Count(r => r.Status == AttendanceStatus.Late);
        var total = records.Count;

        // simple formula: Present=1, Late=0.5, Absent=0
        var score = present + (late * 0.5);
        var percentage = total == 0 ? 0 : (int)Math.Round((score / total) * 100);

        var vm = new StudentCourseReportViewModel
        {
            CourseId = courseId,
            CourseDisplay = $"{course.UniqueCode} - {course.Name} ({course.Batch.Name})",
            Total = total,
            Present = present,
            Absent = absent,
            Late = late,
            Percentage = percentage,
            Rows = records.Select(r => new StudentCourseReportRowViewModel
            {
                Date = r.Date,
                TeacherName = r.MarkedByTeacher.Name,
                IsMakeUpLecture = r.IsMakeUpLecture,
                Status = r.Status
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet("{courseId:int}/export")]
    public async Task<IActionResult> Export(int courseId)
    {
        var studentId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(studentId)) return Unauthorized();

        var enrolled = await _db.Enrollments.AsNoTracking().AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
        if (!enrolled) return Forbid();

        var course = await _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound();

        var rows = await _db.Attendances.AsNoTracking()
            .Where(a => a.StudentId == studentId && a.CourseId == courseId)
            .Include(a => a.MarkedByTeacher)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.MarkedByTeacher.Name)
            .Select(a => new
            {
                a.Date,
                Teacher = a.MarkedByTeacher.Name,
                a.Status,
                a.IsMakeUpLecture
            })
            .ToListAsync();

        var csv = _export.ToCsv(
            fileNameBase: $"my_{course.UniqueCode}_attendance_{DateTime.UtcNow:yyyyMMddHHmmss}",
            headers: new[] { "Date", "Teacher", "Status", "MakeUp" },
            rows: rows.Select(r => (IReadOnlyList<string?>)
                new[] { r.Date.ToString("yyyy-MM-dd"), r.Teacher, ExportService.StatusToString(r.Status), r.IsMakeUpLecture ? "Yes" : "No" }));

        return File(csv.bytes, csv.contentType, csv.fileName);
    }
}
