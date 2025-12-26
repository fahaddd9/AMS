using Attendence_Management_System.Data;
using Attendence_Management_System.Models.Admin.Timetables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Controllers.Admin;

[Authorize(Roles = IdentitySeed.AdminRole)]
[Route("admin/timetables")]
public class TimetablesController : Controller
{
    private readonly ApplicationDbContext _db;

    public TimetablesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? courseId = null)
    {
        await PopulateCoursesAsync(courseId);

        var query = _db.TimetableSlots.AsNoTracking()
            .Include(t => t.Course)
            .ThenInclude(c => c.Batch)
            .AsQueryable();

        if (courseId.HasValue)
            query = query.Where(t => t.CourseId == courseId.Value);

        var items = await query
            .OrderBy(t => t.Course.UniqueCode)
            .ThenBy(t => t.DayOfWeek)
            .Select(t => new TimetableSlotListItemViewModel
            {
                Id = t.Id,
                CourseId = t.CourseId,
                CourseDisplay = $"{t.Course.UniqueCode} - {t.Course.Name} ({t.Course.Batch.Name})",
                DayOfWeek = t.DayOfWeek,
                TimeRange = t.TimeRange
            })
            .ToListAsync();

        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(int? courseId = null)
    {
        await PopulateCoursesAsync(courseId);
        return View("Form", new TimetableSlotFormViewModel { CourseId = courseId });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TimetableSlotFormViewModel model)
    {
        model.TimeRange = string.IsNullOrWhiteSpace(model.TimeRange) ? null : model.TimeRange.Trim();

        await PopulateCoursesAsync(model.CourseId);

        if (!ModelState.IsValid)
            return View("Form", model);

        var courseExists = await _db.Courses.AnyAsync(c => c.Id == model.CourseId);
        if (!courseExists)
        {
            ModelState.AddModelError(nameof(model.CourseId), "Invalid course.");
            return View("Form", model);
        }

        var exists = await _db.TimetableSlots.AnyAsync(t =>
            t.CourseId == model.CourseId &&
            t.DayOfWeek == model.DayOfWeek &&
            t.TimeRange == model.TimeRange);

        if (exists)
        {
            ModelState.AddModelError(string.Empty, "This timetable slot already exists for the selected course.");
            return View("Form", model);
        }

        _db.TimetableSlots.Add(new TimetableSlot
        {
            CourseId = model.CourseId!.Value,
            DayOfWeek = model.DayOfWeek!.Value,
            TimeRange = model.TimeRange
        });

        await _db.SaveChangesAsync();

        TempData["Success"] = "Timetable slot created.";
        return RedirectToAction(nameof(Index), new { courseId = model.CourseId });
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var slot = await _db.TimetableSlots.FirstOrDefaultAsync(t => t.Id == id);
        if (slot == null) return NotFound();

        await PopulateCoursesAsync(slot.CourseId);

        return View("Form", new TimetableSlotFormViewModel
        {
            Id = slot.Id,
            CourseId = slot.CourseId,
            DayOfWeek = slot.DayOfWeek,
            TimeRange = slot.TimeRange
        });
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TimetableSlotFormViewModel model)
    {
        model.TimeRange = string.IsNullOrWhiteSpace(model.TimeRange) ? null : model.TimeRange.Trim();

        await PopulateCoursesAsync(model.CourseId);

        if (!ModelState.IsValid)
            return View("Form", model);

        var slot = await _db.TimetableSlots.FirstOrDefaultAsync(t => t.Id == id);
        if (slot == null) return NotFound();

        var exists = await _db.TimetableSlots.AnyAsync(t =>
            t.Id != id &&
            t.CourseId == model.CourseId &&
            t.DayOfWeek == model.DayOfWeek &&
            t.TimeRange == model.TimeRange);

        if (exists)
        {
            ModelState.AddModelError(string.Empty, "This timetable slot already exists for the selected course.");
            return View("Form", model);
        }

        slot.CourseId = model.CourseId!.Value;
        slot.DayOfWeek = model.DayOfWeek!.Value;
        slot.TimeRange = model.TimeRange;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Timetable slot updated.";
        return RedirectToAction(nameof(Index), new { courseId = slot.CourseId });
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int? courseId = null)
    {
        var slot = await _db.TimetableSlots.FirstOrDefaultAsync(t => t.Id == id);
        if (slot == null) return NotFound();

        _db.TimetableSlots.Remove(slot);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Timetable slot deleted.";
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
}
