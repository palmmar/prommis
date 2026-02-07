using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Prommis.Data;
using Prommis.Models;
using Prommis.Services;

namespace Prommis.Pages.Groups;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly StatisticsService _statistics;

    public DetailsModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, StatisticsService statistics)
    {
        _db = db;
        _userManager = userManager;
        _statistics = statistics;
    }

    public Group? Group { get; private set; }
    public bool IsOwner { get; private set; }
    public bool IsAdmin { get; private set; }
    public IReadOnlyList<GroupMembership> Memberships { get; private set; } = Array.Empty<GroupMembership>();
    public IReadOnlyList<Invitation> ActiveInvites { get; private set; } = Array.Empty<Invitation>();

    public string WeekLabelsJson { get; private set; } = "[]";
    public string WeekValuesJson { get; private set; } = "[]";
    public string MonthLabelsJson { get; private set; } = "[]";
    public string MonthValuesJson { get; private set; } = "[]";
    public string YearLabelsJson { get; private set; } = "[]";
    public string YearValuesJson { get; private set; } = "[]";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        IsAdmin = User.IsInRole("Administrator");

        var hasAccess = await LoadGroupAsync(id, userId);
        if (Group is null)
        {
            return NotFound();
        }
        if (!hasAccess && !IsAdmin)
        {
            return Forbid();
        }

        // If admin is not a member, load group data they wouldn't normally see
        if (!hasAccess && IsAdmin)
        {
            Memberships = Group.Memberships.OrderByDescending(m => m.Role).ThenBy(m => m.User!.UserName).ToList();
            var now = DateTime.UtcNow;
            ActiveInvites = await _db.Invitations
                .AsNoTracking()
                .Where(i => i.GroupId == id && i.UsedAt == null && i.ExpiresAt >= now)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        await LoadChartsAsync(id);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateInviteAsync(int id)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        IsAdmin = User.IsInRole("Administrator");

        var hasAccess = await LoadGroupAsync(id, userId);
        if (Group is null)
        {
            return NotFound();
        }
        if (!hasAccess && !IsAdmin)
        {
            return Forbid();
        }

        if (!IsOwner && !IsAdmin)
        {
            return Forbid();
        }

        var invite = new Invitation
        {
            GroupId = id,
            CreatedById = userId,
            Token = CreateToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        _db.Invitations.Add(invite);
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRemoveMemberAsync(int id, string memberId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        IsAdmin = User.IsInRole("Administrator");

        var hasAccess = await LoadGroupAsync(id, userId);
        if (Group is null)
        {
            return NotFound();
        }
        if ((!hasAccess || !IsOwner) && !IsAdmin)
        {
            return Forbid();
        }

        if (memberId == Group!.OwnerId)
        {
            return BadRequest();
        }

        var toRemove = await _db.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == memberId);

        if (toRemove is null)
        {
            return NotFound();
        }

        _db.GroupMemberships.Remove(toRemove);
        await _db.SaveChangesAsync();

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostTransferOwnershipAsync(int id, string newOwnerId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        var hasAccess = await LoadGroupAsync(id, userId);
        if (Group is null)
        {
            return NotFound();
        }
        if (!hasAccess || !IsOwner)
        {
            return Forbid();
        }

        var newOwner = await _db.GroupMemberships
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == newOwnerId);

        if (newOwner is null)
        {
            return NotFound();
        }

        var group = newOwner.Group!;
        group.OwnerId = newOwnerId;

        var currentOwner = await _db.GroupMemberships
            .FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == userId);

        if (currentOwner is not null)
        {
            currentOwner.Role = GroupRole.Member;
        }

        newOwner.Role = GroupRole.Owner;

        await _db.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadGroupAsync(int id, string userId)
    {
        Group = await _db.Groups
            .Include(g => g.Owner)
            .Include(g => g.Memberships)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (Group is null)
        {
            return false;
        }

        var membership = Group.Memberships.FirstOrDefault(m => m.UserId == userId);
        if (membership is null)
        {
            return false;
        }

        IsOwner = membership.Role == GroupRole.Owner || Group.OwnerId == userId;
        Memberships = Group.Memberships.OrderByDescending(m => m.Role).ThenBy(m => m.User!.UserName).ToList();

        var now = DateTime.UtcNow;
        ActiveInvites = await _db.Invitations
            .AsNoTracking()
            .Where(i => i.GroupId == id && i.UsedAt == null && i.ExpiresAt >= now)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return true;
    }

    private async Task LoadChartsAsync(int id)
    {
        var series = await _statistics.GetGroupSeriesAsync(id, DateTime.Today);
        WeekLabelsJson = JsonSerializer.Serialize(series.Week.Labels);
        WeekValuesJson = JsonSerializer.Serialize(series.Week.Values);
        MonthLabelsJson = JsonSerializer.Serialize(series.Month.Labels);
        MonthValuesJson = JsonSerializer.Serialize(series.Month.Values);
        YearLabelsJson = JsonSerializer.Serialize(series.Year.Labels);
        YearValuesJson = JsonSerializer.Serialize(series.Year.Values);
    }

    private static string CreateToken()
    {
        var data = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(data);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        var base64 = Convert.ToBase64String(input);
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
