using Attendence_Management_System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Attendence_Management_System.Controllers;

[Authorize(Roles = IdentitySeed.AdminRole)]
public class AdminDashboardController : Controller
{
    public IActionResult Index() => View();
}
