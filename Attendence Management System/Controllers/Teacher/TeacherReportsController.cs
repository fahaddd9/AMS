using Attendence_Management_System.Data;
using Attendence_Management_System.Models.Teacher.Reports;
using Attendence_Management_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Controllers.Teacher;

[Authorize(Roles = IdentitySeed.TeacherRole)]
[Route("teacher/reports")]
public class TeacherReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ExportService _export;

    public TeacherReportsController(ApplicationDbContext db, ExportService export)
    {
        _db = db;
        _export = export;
    }

    [HttpGet("course/{courseId:int}")]
    public async Task<IActionResult> Course(int courseId, DateOnly? from = null, DateOnly? to = null)
    {
        var teacherId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(teacherId)) return Unauthorized();

        var assigned = await _db.TeacherAssignments.AnyAsync(a => a.CourseId == courseId && a.TeacherId == teacherId);
        if (!assigned) return Forbid();

        var course = await _db.Courses.AsNoTracking().Include(c => c.Batch).FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound();

        var query = _db.Attendances.AsNoTracking()
            .Where(a => a.CourseId == courseId && a.MarkedByTeacherId == teacherId)
            .Include(a => a.Student)
            .AsQueryable();

        if (from.HasValue) query = query.Where(a => a.Date >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Date <= to.Value);

        var records = await query
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.Student.Name)
            .ToListAsync();

        var present = records.Count(r => r.Status == AttendanceStatus.Present);
        var absent = records.Count(r => r.Status == AttendanceStatus.Absent);
        var late = records.Count(r => r.Status == AttendanceStatus.Late);

        var vm = new TeacherCourseReportViewModel
        {
            CourseId = courseId,
            CourseDisplay = $"{course.UniqueCode} - {course.Name} ({course.Batch.Name})",
            From = from,
            To = to,
            TotalRecords = records.Count,
            Present = present,
            Absent = absent,
            Late = late,
            Rows = records.Select(r => new TeacherCourseReportRowViewModel
            {
                Date = r.Date,
                StudentName = r.Student.Name,
                StudentEmail = r.Student.Email ?? "",
                Status = r.Status,
                IsMakeUpLecture = r.IsMakeUpLecture
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet("course/{courseId:int}/export")]
    public async Task<IActionResult> ExportCourse(int courseId, DateOnly? from = null, DateOnly? to = null)
    {
        var teacherId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(teacherId)) return Unauthorized();

        var assigned = await _db.TeacherAssignments.AnyAsync(a => a.CourseId == courseId && a.TeacherId == teacherId);
        if (!assigned) return Forbid();

        var course = await _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound();

        var query = _db.Attendances.AsNoTracking()
            .Where(a => a.CourseId == courseId && a.MarkedByTeacherId == teacherId)
            .Include(a => a.Student)
            .AsQueryable();

        if (from.HasValue) query = query.Where(a => a.Date >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Date <= to.Value);

        var rows = await query
            .OrderBy(a => a.Date)
            .ThenBy(a => a.Student.Name)
            .Select(a => new
            {
                a.Date,
                StudentName = a.Student.Name,
                StudentEmail = a.Student.Email,
                a.Status,
                a.IsMakeUpLecture
            })
            .ToListAsync();

        var csv = _export.ToCsv(
            fileNameBase: $"course_{course.UniqueCode}_attendance_{DateTime.UtcNow:yyyyMMddHHmmss}",
            headers: new[] { "Date", "Student", "Email", "Status", "MakeUp" },
            rows: rows.Select(r => (IReadOnlyList<string?>)
                new[] { r.Date.ToString("yyyy-MM-dd"), r.StudentName, r.StudentEmail, ExportService.StatusToString(r.Status), r.IsMakeUpLecture ? "Yes" : "No" }));

        return File(csv.bytes, csv.contentType, csv.fileName);
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> Student(string studentId, DateOnly? from = null, DateOnly? to = null)
    {
        var teacherId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(teacherId)) return Unauthorized();

        var student = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == studentId);
        if (student == null) return NotFound();

        // teacher can view only students that are enrolled in teacher's assigned courses
        var teacherCourseIds = await _db.TeacherAssignments.AsNoTracking()
            .Where(a => a.TeacherId == teacherId)
            .Select(a => a.CourseId)
            .ToListAsync();

        var isRelated = await _db.Enrollments.AsNoTracking()
            .AnyAsync(e => e.StudentId == studentId && teacherCourseIds.Contains(e.CourseId));
        if (!isRelated) return Forbid();

        var query = _db.Attendances.AsNoTracking()
            .Where(a => a.StudentId == studentId && a.MarkedByTeacherId == teacherId)
            .Include(a => a.Course)
            .AsQueryable();

        if (from.HasValue) query = query.Where(a => a.Date >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Date <= to.Value);

        var records = await query
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.Course.UniqueCode)
            .ToListAsync();

        var present = records.Count(r => r.Status == AttendanceStatus.Present);
        var absent = records.Count(r => r.Status == AttendanceStatus.Absent);
        var late = records.Count(r => r.Status == AttendanceStatus.Late);

        var vm = new TeacherStudentReportViewModel
        {
            StudentId = student.Id,
            StudentName = student.Name,
            StudentEmail = student.Email ?? "",
            From = from,
            To = to,
            TotalRecords = records.Count,
            Present = present,
            Absent = absent,
            Late = late,
            Rows = records.Select(r => new TeacherStudentReportRowViewModel
            {
                Date = r.Date,
                CourseCode = r.Course.UniqueCode,
                CourseName = r.Course.Name,
                Status = r.Status,
                IsMakeUpLecture = r.IsMakeUpLecture
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet("student/{studentId}/export")]
    public async Task<IActionResult> ExportStudent(string studentId, DateOnly? from = null, DateOnly? to = null)
    {
        var teacherId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(teacherId)) return Unauthorized();

        var student = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == studentId);
        if (student == null) return NotFound();

        var teacherCourseIds = await _db.TeacherAssignments.AsNoTracking()
            .Where(a => a.TeacherId == teacherId)
            .Select(a => a.CourseId)
            .ToListAsync();

        var isRelated = await _db.Enrollments.AsNoTracking()
            .AnyAsync(e => e.StudentId == studentId && teacherCourseIds.Contains(e.CourseId));
        if (!isRelated) return Forbid();

        var query = _db.Attendances.AsNoTracking()
            .Where(a => a.StudentId == studentId && a.MarkedByTeacherId == teacherId)
            .Include(a => a.Course)
            .AsQueryable();

        if (from.HasValue) query = query.Where(a => a.Date >= from.Value);
        if (to.HasValue) query = query.Where(a => a.Date <= to.Value);

        var rows = await query
            .OrderBy(a => a.Date)
            .ThenBy(a => a.Course.UniqueCode)
            .Select(a => new
            {
                a.Date,
                CourseCode = a.Course.UniqueCode,
                CourseName = a.Course.Name,
                a.Status,
                a.IsMakeUpLecture
            })
            .ToListAsync();

        var csv = _export.ToCsv(
            fileNameBase: $"student_{student.Name}_attendance_{DateTime.UtcNow:yyyyMMddHHmmss}",
            headers: new[] { "Date", "CourseCode", "CourseName", "Status", "MakeUp" },
            rows: rows.Select(r => (IReadOnlyList<string?>)
                new[] { r.Date.ToString("yyyy-MM-dd"), r.CourseCode, r.CourseName, ExportService.StatusToString(r.Status), r.IsMakeUpLecture ? "Yes" : "No" }));

        return File(csv.bytes, csv.contentType, csv.fileName);
    }
}
