using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Prommis.Data;

namespace Prommis.Pages.Admin;

[Authorize(Roles = "Administrator")]
public class GroupsModel : PageModel
{
    private readonly ApplicationDbContext _db;

    public GroupsModel(ApplicationDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<GroupRow> Groups { get; private set; } = Array.Empty<GroupRow>();

    public async Task OnGetAsync()
    {
        Groups = await _db.Groups
            .AsNoTracking()
            .Include(g => g.Owner)
            .Select(g => new GroupRow
            {
                Id = g.Id,
                Name = g.Name,
                OwnerName = g.Owner!.UserName ?? "",
                MemberCount = g.Memberships.Count,
            })
            .OrderBy(g => g.Name)
            .ToListAsync();
    }

    public sealed record GroupRow
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string OwnerName { get; init; } = string.Empty;
        public int MemberCount { get; init; }
    }
}
