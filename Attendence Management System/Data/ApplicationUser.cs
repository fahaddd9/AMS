using Microsoft.AspNetCore.Identity;

namespace Attendence_Management_System.Data;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;

    // Student-specific
    public int? BatchId { get; set; }
    public Batch? Batch { get; set; }
}
