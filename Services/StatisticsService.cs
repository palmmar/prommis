using System.Globalization;
using Microsoft.EntityFrameworkCore;
using StegStat.Data;
using StegStat.Models;

namespace StegStat.Services;

public sealed record ChartSeries(IReadOnlyList<string> Labels, IReadOnlyList<int> Values);
public sealed record DashboardSeries(ChartSeries Week, ChartSeries Month, ChartSeries Year);

public class StatisticsService
{
    private readonly ApplicationDbContext _db;
    private readonly CultureInfo _culture = CultureInfo.GetCultureInfo("sv-SE");

    public StatisticsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardSeries> GetUserSeriesAsync(string userId, DateTime today)
    {
        var baseQuery = _db.StepEntries.AsNoTracking().Where(e => e.UserId == userId);
        return await BuildSeriesAsync(baseQuery, today);
    }

    public async Task<DashboardSeries> GetGroupSeriesAsync(int groupId, DateTime today)
    {
        var baseQuery =
            from entry in _db.StepEntries.AsNoTracking()
            join membership in _db.GroupMemberships.AsNoTracking()
                on entry.UserId equals membership.UserId
            where membership.GroupId == groupId
            select entry;

        return await BuildSeriesAsync(baseQuery, today);
    }

    private async Task<DashboardSeries> BuildSeriesAsync(IQueryable<StepEntry> baseQuery, DateTime today)
    {
        var weekStart = today.Date.AddDays(-6);
        var weekEnd = today.Date;
        var weekData = await DailyTotalsAsync(baseQuery, weekStart, weekEnd);
        var weekLabels = new List<string>();
        var weekValues = new List<int>();
        for (var date = weekStart; date <= weekEnd; date = date.AddDays(1))
        {
            weekLabels.Add(date.ToString("ddd d/M", _culture));
            weekValues.Add(weekData.TryGetValue(date.Date, out var value) ? value : 0);
        }

        var monthStart = new DateTime(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var monthData = await DailyTotalsAsync(baseQuery, monthStart, monthEnd);
        var monthLabels = new List<string>();
        var monthValues = new List<int>();
        for (var date = monthStart; date <= monthEnd; date = date.AddDays(1))
        {
            monthLabels.Add(date.Day.ToString(_culture));
            monthValues.Add(monthData.TryGetValue(date.Date, out var value) ? value : 0);
        }

        var yearStart = new DateTime(today.Year, today.Month, 1).AddMonths(-11);
        var yearEnd = new DateTime(today.Year, today.Month, 1).AddMonths(1).AddDays(-1);
        var yearData = await MonthlyTotalsAsync(baseQuery, yearStart, yearEnd);
        var yearLabels = new List<string>();
        var yearValues = new List<int>();
        for (var date = yearStart; date <= yearEnd; date = date.AddMonths(1))
        {
            var key = (date.Year, date.Month);
            yearLabels.Add(date.ToString("MMM yyyy", _culture));
            yearValues.Add(yearData.TryGetValue(key, out var value) ? value : 0);
        }

        return new DashboardSeries(
            new ChartSeries(weekLabels, weekValues),
            new ChartSeries(monthLabels, monthValues),
            new ChartSeries(yearLabels, yearValues));
    }

    private async Task<Dictionary<DateTime, int>> DailyTotalsAsync(IQueryable<StepEntry> baseQuery, DateTime start, DateTime end)
    {
        var results = await baseQuery
            .Where(e => e.Date >= start && e.Date <= end)
            .GroupBy(e => e.Date)
            .Select(g => new { Date = g.Key, Steps = g.Sum(x => x.Steps) })
            .ToListAsync();

        return results.ToDictionary(x => x.Date.Date, x => x.Steps);
    }

    private async Task<Dictionary<(int Year, int Month), int>> MonthlyTotalsAsync(
        IQueryable<StepEntry> baseQuery,
        DateTime start,
        DateTime end)
    {
        var results = await baseQuery
            .Where(e => e.Date >= start && e.Date <= end)
            .GroupBy(e => new { e.Date.Year, e.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Steps = g.Sum(x => x.Steps) })
            .ToListAsync();

        return results.ToDictionary(x => (x.Year, x.Month), x => x.Steps);
    }
}
