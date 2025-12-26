using Attendence_Management_System.Data;
using Attendence_Management_System.Models.Teacher.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Controllers.Teacher;

[Authorize(Roles = IdentitySeed.TeacherRole)]
[Route("teacher/attendance")]
public class TeacherAttendanceController : Controller
{
    private readonly ApplicationDbContext _db;

    public TeacherAttendanceController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var teacherId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(teacherId)) return Unauthorized();

        // NOTE: EF Core can't apply Include after projecting with Select(a => a.Course)
        var courses = await _db.TeacherAssignments
            .AsNoTracking()
            .Where(a => a.TeacherId == teacherId)
            .Include(a => a.Course)
                .ThenInclude(c => c.Batch)
            .Include(a => a.Course)
                .ThenInclude(c => c.Enrollments)
            .Include(a => a.Course)
                .ThenInclude(c => c.TimetableSlots)
            .Select(a => a.Course)
            .Distinct()
            .OrderBy(c => c.UniqueCode)
            .Select(c => new TeacherCourseListItemViewModel
            {
                CourseId = c.Id,
                Code = c.UniqueCode,
                Name = c.Name,
                BatchName = c.Batch.Name,
                EnrolledStudents = c.Enrollments.Count,
                TimetableDaysCount = c.TimetableSlots.Select(t => t.DayOfWeek).Distinct().Count()
            })
            .ToListAsync();

        return View(courses);
    }

    [HttpGet("mark")]
    public async Task<IActionResult> Mark(int courseId, DateOnly? date = null)
    {
        var teacherId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(teacherId)) return Unauthorized();

        // must be assigned
        var assigned = await _db.TeacherAssignments.AnyAsync(a => a.CourseId == courseId && a.TeacherId == teacherId);
        if (!assigned) return Forbid();

        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        var course = await _db.Courses
            .AsNoTracking()
            .Include(c => c.Batch)
            .FirstOrDefaultAsync(c => c.Id == courseId);
        if (course == null) return NotFound();

        // enrolled students for that course
        var students = await _db.Enrollments.AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => e.Student)
            .OrderBy(s => s.Name)
            .ToListAsync();

        // prefill existing attendance for this teacher/course/date
        var existing = await _db.Attendances.AsNoTracking()
            .Where(a => a.CourseId == courseId && a.Date == selectedDate && a.MarkedByTeacherId == teacherId)
            .ToListAsync();

        var rows = students.Select(s =>
        {
            var ex = existing.FirstOrDefault(a => a.StudentId == s.Id);
            return new AttendanceMarkRowViewModel
            {
                StudentId = s.Id,
                StudentName = s.Name,
                StudentEmail = s.Email ?? "",
                Status = ex?.Status ?? AttendanceStatus.Present
            };
        }).ToList();

        var vm = new AttendanceMarkViewModel
        {
            CourseId = courseId,
            Date = selectedDate,
            Rows = rows
        };

        ViewBag.CourseDisplay = $"{course.UniqueCode} - {course.Name} ({course.Batch.Name})";
        ViewBag.ScheduledDays = await _db.TimetableSlots.AsNoTracking().Where(t => t.CourseId == courseId).Select(t => t.DayOfWeek).Distinct().ToListAsync();

        return View(vm);
    }

    [HttpPost("mark")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Mark(AttendanceMarkViewModel model)
    {
        var teacherId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(teacherId)) return Unauthorized();

        if (!ModelState.IsValid)
        {
            await PopulateMarkViewBags(model, teacherId);
            return View(model);
        }

        var courseId = model.CourseId!.Value;

        // must be assigned
        var assigned = await _db.TeacherAssignments.AnyAsync(a => a.CourseId == courseId && a.TeacherId == teacherId);
        if (!assigned) return Forbid();

        // timetable enforcement (day-of-week only)
        var scheduledDays = await _db.TimetableSlots.AsNoTracking()
            .Where(t => t.CourseId == courseId)
            .Select(t => t.DayOfWeek)
            .Distinct()
            .ToListAsync();

        if (scheduledDays.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "No timetable exists for this course—contact Admin.");
            await PopulateMarkViewBags(model, teacherId);
            return View(model);
        }

        var day = model.Date.DayOfWeek;
        var isScheduledDay = scheduledDays.Contains(day);

        if (!isScheduledDay && !model.IsMakeUpLecture)
        {
            ModelState.AddModelError(string.Empty, "Attendance can only be marked on scheduled days. If this is a make-up lecture, check the Make-up lecture option.");
            await PopulateMarkViewBags(model, teacherId);
            return View(model);
        }

        // upsert per student/course/date/teacher
        foreach (var row in model.Rows)
        {
            var existing = await _db.Attendances.FirstOrDefaultAsync(a =>
                a.CourseId == courseId &&
                a.Date == model.Date &&
                a.StudentId == row.StudentId &&
                a.MarkedByTeacherId == teacherId);

            if (existing == null)
            {
                _db.Attendances.Add(new Attendance
                {
                    CourseId = courseId,
                    Date = model.Date,
                    StudentId = row.StudentId,
                    MarkedByTeacherId = teacherId,
                    Status = row.Status,
                    IsMakeUpLecture = model.IsMakeUpLecture
                });
            }
            else
            {
                existing.Status = row.Status;
                existing.IsMakeUpLecture = model.IsMakeUpLecture;
            }
        }

        await _db.SaveChangesAsync();

        TempData["Success"] = "Attendance saved.";
        return RedirectToAction(nameof(Mark), new { courseId, date = model.Date.ToString("yyyy-MM-dd") });
    }

    private async Task PopulateMarkViewBags(AttendanceMarkViewModel model, string teacherId)
    {
        if (model.CourseId == null) return;

        // must be assigned (avoid leaking course info)
        var assigned = await _db.TeacherAssignments.AnyAsync(a => a.CourseId == model.CourseId.Value && a.TeacherId == teacherId);
        if (!assigned) return;

        var course = await _db.Courses.AsNoTracking().Include(c => c.Batch).FirstOrDefaultAsync(c => c.Id == model.CourseId.Value);
        ViewBag.CourseDisplay = course == null ? "" : $"{course.UniqueCode} - {course.Name} ({course.Batch.Name})";
        ViewBag.ScheduledDays = await _db.TimetableSlots.AsNoTracking().Where(t => t.CourseId == model.CourseId.Value).Select(t => t.DayOfWeek).Distinct().ToListAsync();

        // repopulate student names/emails
        var students = await _db.Enrollments.AsNoTracking()
            .Where(e => e.CourseId == model.CourseId.Value)
            .Select(e => e.Student)
            .OrderBy(s => s.Name)
            .ToListAsync();

        var map = students.ToDictionary(s => s.Id);
        foreach (var r in model.Rows)
        {
            if (map.TryGetValue(r.StudentId, out var s))
            {
                r.StudentName = s.Name;
                r.StudentEmail = s.Email ?? "";
            }
        }
    }
}
