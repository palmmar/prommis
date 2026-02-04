using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StegStat.Data;
using StegStat.Models;

namespace StegStat.Pages.Groups;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public IReadOnlyList<GroupOverview> Groups { get; private set; } = Array.Empty<GroupOverview>();

    public async Task OnGetAsync()
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return;
        }

        Groups = await _db.GroupMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => new GroupOverview
            {
                GroupId = m.GroupId,
                Name = m.Group!.Name,
                OwnerName = m.Group.Owner!.UserName ?? "",
                MemberCount = m.Group.Memberships.Count
            })
            .OrderBy(g => g.Name)
            .ToListAsync();
    }

    public sealed record GroupOverview
    {
        public int GroupId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string OwnerName { get; init; } = string.Empty;
        public int MemberCount { get; init; }
    }
}
