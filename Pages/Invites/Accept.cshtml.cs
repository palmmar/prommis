using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StegStat.Data;
using StegStat.Models;

namespace StegStat.Pages.Invites;

[Authorize]
public class AcceptModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AcceptModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public Invitation? Invite { get; private set; }
    public string? StatusMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(string token)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        Invite = await _db.Invitations
            .Include(i => i.Group)
            .ThenInclude(g => g!.Owner)
            .FirstOrDefaultAsync(i => i.Token == token);

        if (Invite is null || !Invite.IsActive(DateTime.UtcNow))
        {
            StatusMessage = "Inbjudningen är ogiltig eller har gått ut.";
            return Page();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string token)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        Invite = await _db.Invitations
            .Include(i => i.Group)
            .FirstOrDefaultAsync(i => i.Token == token);

        if (Invite is null || !Invite.IsActive(DateTime.UtcNow))
        {
            StatusMessage = "Inbjudningen är ogiltig eller har gått ut.";
            return Page();
        }

        var alreadyMember = await _db.GroupMemberships
            .AnyAsync(m => m.GroupId == Invite.GroupId && m.UserId == userId);

        if (!alreadyMember)
        {
            var membership = new GroupMembership
            {
                GroupId = Invite.GroupId,
                UserId = userId,
                Role = GroupRole.Member
            };

            _db.GroupMemberships.Add(membership);
        }

        Invite.UsedAt = DateTime.UtcNow;
        Invite.AcceptedById = userId;

        await _db.SaveChangesAsync();

        return RedirectToPage("/Groups/Details", new { id = Invite.GroupId });
    }
}
