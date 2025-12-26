using Attendence_Management_System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendence_Management_System.Controllers;

[Authorize(Roles = IdentitySeed.TeacherRole)]
public class TeacherDashboardController : Controller
{
    public IActionResult Index() => View();
}
