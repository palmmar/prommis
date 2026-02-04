using Microsoft.AspNetCore.Identity;

namespace Prommis.Models;

public class ApplicationUser : IdentityUser
{
    public ICollection<GroupMembership> GroupMemberships { get; set; } = new List<GroupMembership>();
    public ICollection<StepEntry> StepEntries { get; set; } = new List<StepEntry>();
}
