using Attendence_Management_System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendence_Management_System.Controllers;

[Authorize]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        if (User.IsInRole(IdentitySeed.AdminRole))
            return RedirectToAction("Index", "AdminDashboard");

        if (User.IsInRole(IdentitySeed.TeacherRole))
            return RedirectToAction("Index", "TeacherDashboard");

        if (User.IsInRole(IdentitySeed.StudentRole))
            return RedirectToAction("Index", "StudentDashboard");

        return View("UnknownRole");
    }
}
