using Attendence_Management_System.Data;
using Attendence_Management_System.Models.Admin.Batches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Controllers.Admin;

[Authorize(Roles = IdentitySeed.AdminRole)]
[Route("admin/batches")]
public class BatchesController : Controller
{
    private readonly ApplicationDbContext _db;

    public BatchesController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var items = await _db.Batches
            .OrderByDescending(b => b.Id)
            .Select(b => new BatchListItemViewModel
            {
                Id = b.Id,
                Name = b.Name,
                StudentsCount = b.Students.Count,
                CoursesCount = b.Courses.Count
            })
            .ToListAsync();

        return View(items);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View("Form", new BatchFormViewModel());
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BatchFormViewModel model)
    {
        model.Name = (model.Name ?? string.Empty).Trim();

        if (!ModelState.IsValid)
            return View("Form", model);

        var exists = await _db.Batches.AnyAsync(b => b.Name == model.Name);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.Name), "This batch name already exists.");
            return View("Form", model);
        }

        _db.Batches.Add(new Batch { Name = model.Name });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Batch created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == id);
        if (batch == null) return NotFound();

        return View("Form", new BatchFormViewModel { Id = batch.Id, Name = batch.Name });
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BatchFormViewModel model)
    {
        model.Name = (model.Name ?? string.Empty).Trim();

        if (!ModelState.IsValid)
            return View("Form", model);

        var batch = await _db.Batches.FirstOrDefaultAsync(b => b.Id == id);
        if (batch == null) return NotFound();

        var exists = await _db.Batches.AnyAsync(b => b.Id != id && b.Name == model.Name);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.Name), "This batch name already exists.");
            return View("Form", model);
        }

        batch.Name = model.Name;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Batch updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var batch = await _db.Batches
            .Include(b => b.Students)
            .Include(b => b.Courses)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (batch == null) return NotFound();

        // Prevent deletion if dependent data exists
        if (batch.Students.Count > 0 || batch.Courses.Count > 0)
        {
            TempData["Error"] = "Cannot delete this batch because it has students or courses assigned.";
            return RedirectToAction(nameof(Index));
        }

        _db.Batches.Remove(batch);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Batch deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
