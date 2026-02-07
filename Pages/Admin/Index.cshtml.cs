using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Prommis.Data;
using Prommis.Models;

namespace Prommis.Pages.Admin;

[Authorize(Roles = "Administrator")]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public IndexModel(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public IReadOnlyList<UserRow> Users { get; private set; } = Array.Empty<UserRow>();

    public async Task OnGetAsync()
    {
        var allUsers = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .ToListAsync();

        var groupCounts = await _db.GroupMemberships
            .GroupBy(m => m.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var rows = new List<UserRow>();
        foreach (var user in allUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            groupCounts.TryGetValue(user.Id, out var count);
            rows.Add(new UserRow
            {
                Id = user.Id,
                Email = user.Email ?? "",
                Role = roles.FirstOrDefault() ?? "User",
                GroupCount = count,
            });
        }

        Users = rows;
    }

    public sealed record UserRow
    {
        public string Id { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string Role { get; init; } = string.Empty;
        public int GroupCount { get; init; }
    }
}
