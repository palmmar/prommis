using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Prommis.Data;
using Prommis.Models;
using Prommis.Services;

namespace Prommis.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly StatisticsService _statistics;

    public IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager, StatisticsService statistics)
    {
        _db = db;
        _userManager = userManager;
        _statistics = statistics;
    }

    public bool IsSignedIn { get; private set; }
    public IReadOnlyList<StepEntry> TodayEntries { get; private set; } = Array.Empty<StepEntry>();
    public int TodayTotalSteps { get; private set; }

    public string WeekLabelsJson { get; private set; } = "[]";
    public string WeekValuesJson { get; private set; } = "[]";
    public string MonthLabelsJson { get; private set; } = "[]";
    public string MonthValuesJson { get; private set; } = "[]";
    public string YearLabelsJson { get; private set; } = "[]";
    public string YearValuesJson { get; private set; } = "[]";

    public async Task OnGetAsync()
    {
        await LoadDashboardAsync();
    }

    public async Task<IActionResult> OnPostAddStepAsync(int steps)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        if (steps <= 0)
        {
            ModelState.AddModelError(string.Empty, "Ange ett giltigt antal steg.");
            await LoadDashboardAsync();
            return Page();
        }

        var entry = new StepEntry
        {
            UserId = userId,
            Date = DateTime.Today,
            Steps = steps
        };

        _db.StepEntries.Add(entry);
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditStepAsync(int entryId, int steps)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        if (steps <= 0)
        {
            ModelState.AddModelError(string.Empty, "Ange ett giltigt antal steg.");
            await LoadDashboardAsync();
            return Page();
        }

        var entry = await _db.StepEntries
            .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        if (entry.Date.Date != DateTime.Today)
        {
            return Forbid();
        }

        entry.Steps = steps;
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteStepAsync(int entryId)
    {
        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return Challenge();
        }

        var entry = await _db.StepEntries
            .FirstOrDefaultAsync(e => e.Id == entryId && e.UserId == userId);

        if (entry is null)
        {
            return NotFound();
        }

        if (entry.Date.Date != DateTime.Today)
        {
            return Forbid();
        }

        _db.StepEntries.Remove(entry);
        await _db.SaveChangesAsync();

        return RedirectToPage();
    }

    private async Task LoadDashboardAsync()
    {
        IsSignedIn = User.Identity?.IsAuthenticated ?? false;
        if (!IsSignedIn)
        {
            return;
        }

        var userId = _userManager.GetUserId(User);
        if (userId is null)
        {
            return;
        }

        var today = DateTime.Today;
        TodayEntries = await _db.StepEntries
            .AsNoTracking()
            .Where(e => e.UserId == userId && e.Date == today)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
        TodayTotalSteps = TodayEntries.Sum(e => e.Steps);

        var series = await _statistics.GetUserSeriesAsync(userId, today);
        WeekLabelsJson = JsonSerializer.Serialize(series.Week.Labels);
        WeekValuesJson = JsonSerializer.Serialize(series.Week.Values);
        MonthLabelsJson = JsonSerializer.Serialize(series.Month.Labels);
        MonthValuesJson = JsonSerializer.Serialize(series.Month.Values);
        YearLabelsJson = JsonSerializer.Serialize(series.Year.Labels);
        YearValuesJson = JsonSerializer.Serialize(series.Year.Values);
    }
}
