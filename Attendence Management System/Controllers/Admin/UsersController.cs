using Attendence_Management_System.Data;
using Attendence_Management_System.Models.Admin.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Attendence_Management_System.Controllers.Admin;

[Authorize(Roles = IdentitySeed.AdminRole)]
[Route("admin/users")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? role = null)
    {
        role ??= "All";

        var usersQuery = _db.Users.AsNoTracking().Include(u => u.Batch).AsQueryable();
        var users = await usersQuery.OrderBy(u => u.Email).ToListAsync();

        var result = new List<UserListItemViewModel>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            var primaryRole = roles.FirstOrDefault() ?? "";

            if (role != "All" && !string.Equals(primaryRole, role, StringComparison.OrdinalIgnoreCase))
                continue;

            result.Add(new UserListItemViewModel
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email ?? "",
                Role = primaryRole,
                BatchName = u.Batch?.Name
            });
        }

        ViewBag.SelectedRole = role;
        return View(result);
    }

    [HttpGet("{id}/edit")]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _db.Users.Include(u => u.Batch).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "";

        await PopulateBatchesAsync();

        return View(new EditUserViewModel
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email ?? "",
            Role = role,
            BatchId = user.BatchId
        });
    }

    [HttpPost("{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, EditUserViewModel model)
    {
        if (id != model.Id) return BadRequest();

        model.Name = (model.Name ?? string.Empty).Trim();
        model.Email = (model.Email ?? string.Empty).Trim();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "";

        model.Role = role; // role is informational; we are not changing roles in this screen

        await PopulateBatchesAsync();

        if (!ModelState.IsValid)
            return View(model);

        // Email uniqueness
        var emailOwner = await _userManager.FindByEmailAsync(model.Email);
        if (emailOwner != null && emailOwner.Id != user.Id)
        {
            ModelState.AddModelError(nameof(model.Email), "Email already exists.");
            return View(model);
        }

        // Batch rules: only students must have batch; teachers/admin must not
        if (string.Equals(role, IdentitySeed.StudentRole, StringComparison.OrdinalIgnoreCase))
        {
            if (model.BatchId == null)
            {
                ModelState.AddModelError(nameof(model.BatchId), "Batch is required for students.");
                return View(model);
            }

            var batchExists = await _db.Batches.AnyAsync(b => b.Id == model.BatchId);
            if (!batchExists)
            {
                ModelState.AddModelError(nameof(model.BatchId), "Invalid batch.");
                return View(model);
            }

            user.BatchId = model.BatchId;
        }
        else
        {
            user.BatchId = null;
        }

        user.Name = model.Name;

        // If email changes, update both Email and UserName for login
        if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = model.Email;
            user.UserName = model.Email;
            user.NormalizedEmail = null;
            user.NormalizedUserName = null;
        }

        // Optional password reset
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);
            if (!resetResult.Succeeded)
            {
                foreach (var e in resetResult.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }
        }

        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            foreach (var e in update.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        TempData["Success"] = "User updated successfully.";
        return RedirectToAction(nameof(Index), new { role = string.IsNullOrWhiteSpace(role) ? "All" : role });
    }

    [HttpGet("teachers/create")]
    public IActionResult CreateTeacher() => View(new CreateTeacherViewModel());

    [HttpPost("teachers/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTeacher(CreateTeacherViewModel model)
    {
        model.Email = (model.Email ?? string.Empty).Trim();
        model.Name = (model.Name ?? string.Empty).Trim();

        if (!ModelState.IsValid)
            return View(model);

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            Name = model.Name,
            Email = model.Email,
            UserName = model.Email,
            EmailConfirmed = true
        };

        var created = await _userManager.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            foreach (var e in created.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, IdentitySeed.TeacherRole);

        TempData["Success"] = "Teacher created successfully.";
        return RedirectToAction(nameof(Index), new { role = IdentitySeed.TeacherRole });
    }

    [HttpGet("students/create")]
    public async Task<IActionResult> CreateStudent()
    {
        await PopulateBatchesAsync();
        return View(new CreateStudentViewModel());
    }

    [HttpPost("students/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStudent(CreateStudentViewModel model)
    {
        model.Email = (model.Email ?? string.Empty).Trim();
        model.Name = (model.Name ?? string.Empty).Trim();

        await PopulateBatchesAsync();

        if (!ModelState.IsValid)
            return View(model);

        var batchExists = await _db.Batches.AnyAsync(b => b.Id == model.BatchId);
        if (!batchExists)
        {
            ModelState.AddModelError(nameof(model.BatchId), "Invalid batch.");
            return View(model);
        }

        var existing = await _userManager.FindByEmailAsync(model.Email);
        if (existing != null)
        {
            ModelState.AddModelError(nameof(model.Email), "Email already exists.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            Name = model.Name,
            Email = model.Email,
            UserName = model.Email,
            EmailConfirmed = true,
            BatchId = model.BatchId
        };

        var created = await _userManager.CreateAsync(user, model.Password);
        if (!created.Succeeded)
        {
            foreach (var e in created.Errors)
                ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, IdentitySeed.StudentRole);

        TempData["Success"] = "Student created successfully.";
        return RedirectToAction(nameof(Index), new { role = IdentitySeed.StudentRole });
    }

    [HttpPost("{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, string? role = null)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        // enforce single admin cannot be deleted
        if (await _userManager.IsInRoleAsync(user, IdentitySeed.AdminRole))
        {
            TempData["Error"] = "Admin user cannot be deleted.";
            return RedirectToAction(nameof(Index), new { role = role ?? "All" });
        }

        // Dependent data checks (avoid orphaning data)
        var hasEnrollments = await _db.Enrollments.AnyAsync(e => e.StudentId == id);
        var hasAttendanceAsStudent = await _db.Attendances.AnyAsync(a => a.StudentId == id);
        var hasAttendanceAsTeacher = await _db.Attendances.AnyAsync(a => a.MarkedByTeacherId == id);
        var hasTeacherAssignments = await _db.TeacherAssignments.AnyAsync(t => t.TeacherId == id);

        if (hasEnrollments || hasAttendanceAsStudent || hasAttendanceAsTeacher || hasTeacherAssignments)
        {
            TempData["Error"] = "Cannot delete this user because there are related records (enrollments/attendance/assignments).";
            return RedirectToAction(nameof(Index), new { role = role ?? "All" });
        }

        // remove refresh tokens
        var tokens = await _db.RefreshTokens.Where(t => t.UserId == id).ToListAsync();
        _db.RefreshTokens.RemoveRange(tokens);
        await _db.SaveChangesAsync();

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            TempData["Error"] = string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index), new { role = role ?? "All" });
        }

        TempData["Success"] = "User deleted successfully.";
        return RedirectToAction(nameof(Index), new { role = role ?? "All" });
    }

    private async Task PopulateBatchesAsync()
    {
        var batches = await _db.Batches.AsNoTracking().OrderByDescending(b => b.Id).ToListAsync();
        ViewBag.Batches = batches.Select(b => new SelectListItem(b.Name, b.Id.ToString())).ToList();
    }
}
