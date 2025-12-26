using Attendence_Management_System.Data;
using Attendence_Management_System.Models.Admin.Courses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Controllers.Admin;

[Authorize(Roles = IdentitySeed.AdminRole)]
[Route("admin/courses")]
public class CoursesController : Controller
{
    private readonly ApplicationDbContext _db;

    public CoursesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? batchId = null)
    {
        var query = _db.Courses.AsNoTracking()
            .Include(c => c.Batch)
            .Include(c => c.TeacherAssignments)
            .Include(c => c.Enrollments)
            .AsQueryable();

        if (batchId.HasValue)
            query = query.Where(c => c.BatchId == batchId.Value);

        var items = await query
            .OrderBy(c => c.UniqueCode)
            .Select(c => new CourseListItemViewModel
            {
                Id = c.Id,
                UniqueCode = c.UniqueCode,
                Name = c.Name,
                CreditHours = c.CreditHours,
                BatchName = c.Batch.Name,
                TeachersCount = c.TeacherAssignments.Count,
                EnrollmentsCount = c.Enrollments.Count
            })
            .ToListAsync();

        await PopulateBatchesAsync(batchId);
        return View(items);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create()
    {
        await PopulateBatchesAsync(null);
        return View("Form", new CourseFormViewModel { CreditHours = 3 });
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseFormViewModel model)
    {
        model.UniqueCode = (model.UniqueCode ?? string.Empty).Trim();
        model.Name = (model.Name ?? string.Empty).Trim();

        await PopulateBatchesAsync(model.BatchId);

        if (!ModelState.IsValid)
            return View("Form", model);

        var batchExists = await _db.Batches.AnyAsync(b => b.Id == model.BatchId);
        if (!batchExists)
        {
            ModelState.AddModelError(nameof(model.BatchId), "Invalid batch.");
            return View("Form", model);
        }

        var codeExists = await _db.Courses.AnyAsync(c => c.UniqueCode == model.UniqueCode);
        if (codeExists)
        {
            ModelState.AddModelError(nameof(model.UniqueCode), "Course code already exists.");
            return View("Form", model);
        }

        _db.Courses.Add(new Course
        {
            UniqueCode = model.UniqueCode,
            Name = model.Name,
            CreditHours = model.CreditHours,
            BatchId = model.BatchId!.Value
        });

        await _db.SaveChangesAsync();
        TempData["Success"] = "Course created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);
        if (course == null) return NotFound();

        await PopulateBatchesAsync(course.BatchId);

        return View("Form", new CourseFormViewModel
        {
            Id = course.Id,
            UniqueCode = course.UniqueCode,
            Name = course.Name,
            CreditHours = course.CreditHours,
            BatchId = course.BatchId
        });
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CourseFormViewModel model)
    {
        model.UniqueCode = (model.UniqueCode ?? string.Empty).Trim();
        model.Name = (model.Name ?? string.Empty).Trim();

        await PopulateBatchesAsync(model.BatchId);

        if (!ModelState.IsValid)
            return View("Form", model);

        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);
        if (course == null) return NotFound();

        var batchExists = await _db.Batches.AnyAsync(b => b.Id == model.BatchId);
        if (!batchExists)
        {
            ModelState.AddModelError(nameof(model.BatchId), "Invalid batch.");
            return View("Form", model);
        }

        var codeExists = await _db.Courses.AnyAsync(c => c.Id != id && c.UniqueCode == model.UniqueCode);
        if (codeExists)
        {
            ModelState.AddModelError(nameof(model.UniqueCode), "Course code already exists.");
            return View("Form", model);
        }

        course.UniqueCode = model.UniqueCode;
        course.Name = model.Name;
        course.CreditHours = model.CreditHours;
        course.BatchId = model.BatchId!.Value;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Course updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var course = await _db.Courses
            .Include(c => c.Enrollments)
            .Include(c => c.TeacherAssignments)
            .Include(c => c.TimetableSlots)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (course == null) return NotFound();

        if (course.Enrollments.Count > 0)
        {
            TempData["Error"] = "Cannot delete course because students are enrolled.";
            return RedirectToAction(nameof(Index));
        }

        // allow delete; cascade doesn't exist for some relationships
        _db.TeacherAssignments.RemoveRange(course.TeacherAssignments);
        _db.TimetableSlots.RemoveRange(course.TimetableSlots);
        _db.Courses.Remove(course);

        await _db.SaveChangesAsync();
        TempData["Success"] = "Course deleted successfully.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateBatchesAsync(int? selected)
    {
        var batches = await _db.Batches.AsNoTracking().OrderByDescending(b => b.Id).ToListAsync();
        ViewBag.Batches = batches.Select(b => new SelectListItem(b.Name, b.Id.ToString(), b.Id == selected)).ToList();
    }
}
