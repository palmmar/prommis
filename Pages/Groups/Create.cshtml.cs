using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Prommis.Data;
using Prommis.Models;

namespace Prommis.Pages.Groups;

[Authorize]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [BindProperty]
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        var group = new Group
        {
            Name = Name.Trim(),
            OwnerId = userId
        };

        var membership = new GroupMembership
        {
            Group = group,
            UserId = userId,
            Role = GroupRole.Owner
        };

        _db.Groups.Add(group);
        _db.GroupMemberships.Add(membership);
        await _db.SaveChangesAsync();

        return RedirectToPage("/Groups/Details", new { id = group.Id });
    }
}
