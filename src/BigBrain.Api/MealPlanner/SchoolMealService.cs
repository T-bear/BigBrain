using System.Globalization;

namespace BigBrain.Api.MealPlanner;

public sealed record SchoolMealOptions
{
    public bool Enabled { get; init; }
    public bool RosenfeldtEnabled { get; init; }
    public bool MusikugglanEnabled { get; init; }
}

// Two bounded process-local entries. No scheduler, persistence or cross-module writes.
public sealed class SchoolMealService(MealPlannerOptions options, SchoolMealSourceClient source, TimeProvider clock) : IDisposable
{
    private sealed class Entry : IDisposable
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public DateTimeOffset NextAttempt;
        public DateTimeOffset? CheckedAt;
        public DateTimeOffset? ValidatedAt;
        public SchoolMealWeek? Menu;
        public string? ETag;
        public DateTimeOffset? Modified;
        public string Status = "unavailable";
        public bool AllowStale;
        public void ClearMenu()
        {
            Menu = null;
            ValidatedAt = null;
            ETag = null;
            Modified = null;
            AllowStale = false;
        }
        public void Dispose() => Gate.Dispose();
    }
    private readonly Entry rosenfeldt = new();
    private readonly Entry musikugglan = new();
    private static readonly TimeZoneInfo Stockholm = TimeZoneInfo.FindSystemTimeZoneById("Europe/Stockholm");

    public (int Year, int Week) CurrentWeek()
    {
        var local = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Stockholm).DateTime;
        return (ISOWeek.GetYear(local), ISOWeek.GetWeekOfYear(local));
    }

    public async Task<SchoolMealWeek> GetAsync(string school, int year, int week, CancellationToken token)
    {
        if (!SchoolMeals.KnownSchool(school) || !SchoolMeals.ValidWeek(year, week)) throw new ArgumentException("Invalid school/week.");
        SchoolMealWeek Empty(string status) => SchoolMeals.Create(school, year, week, status, "unknown", false, []);
        var config = options.SchoolMeals;
        if (!config.Enabled || !(school == SchoolMeals.Rosenfeldt ? config.RosenfeldtEnabled : config.MusikugglanEnabled))
            return Empty("offline");
        if (CurrentWeek() != (year, week)) return Empty("notPublished");
        var entry = school == SchoolMeals.Rosenfeldt ? rosenfeldt : musikugglan;
        await entry.Gate.WaitAsync(token);
        try
        {
            if (CurrentWeek() != (year, week)) return Empty("notPublished");
            var now = clock.GetUtcNow();
            if (entry.Menu is { } previous && (previous.Year != year || previous.Week != week))
            {
                entry.ClearMenu();
                entry.Status = "notPublished";
            }
            if (entry.Menu is not null && now - entry.ValidatedAt > TimeSpan.FromHours(24))
            {
                // Expire content and its validators on access, even during a long Retry-After.
                // Keep NextAttempt/CheckedAt: eviction must never authorize another source call.
                entry.ClearMenu();
                if (entry.Status == "published") entry.Status = "unavailable";
            }
            if (now >= entry.NextAttempt)
            {
                entry.NextAttempt = now.AddHours(6); // Includes failed/cancelled attempts and week rollover.
                entry.CheckedAt = now;
                var response = await source.GetAsync(school, entry.ETag, entry.Modified, token);
                entry.Status = response.Status;
                if (response.RetryAt > entry.NextAttempt) entry.NextAttempt = response.RetryAt.Value;
                if (response.Status == "cachePolicy")
                {
                    entry.ClearMenu();
                }
                else if (response.Status == "notModified")
                {
                    if (entry.Menu is null || (entry.ETag is null && entry.Modified is null)) entry.Status = "invalid";
                    else
                    {
                        entry.Status = entry.Menu.Status;
                        entry.ValidatedAt = clock.GetUtcNow();
                        entry.AllowStale &= response.AllowStale;
                    }
                }
                else if (response.Content is { } content)
                {
                    var parsed = school == SchoolMeals.Rosenfeldt
                        ? SchoolMeals.ParseRss(content, year, week)
                        : MusikugglanSchoolMeals.Parse(content, year, week);
                    entry.Status = parsed.Status;
                    if (parsed.Status != "invalid")
                    {
                        entry.ValidatedAt = clock.GetUtcNow();
                        entry.Menu = parsed with { FetchedAtUtc = entry.ValidatedAt, SourceUpdatedAtUtc = response.Modified };
                        entry.ETag = response.ETag;
                        entry.Modified = response.Modified;
                        entry.AllowStale = response.AllowStale;
                    }
                }
            }
            if (CurrentWeek() != (year, week)) return Empty("notPublished");
            var failed = entry.Status is "invalid" or "unavailable" or "cachePolicy";
            var age = clock.GetUtcNow() - entry.ValidatedAt;
            var stale = failed && entry.AllowStale && age <= TimeSpan.FromHours(24) && entry.Menu?.Status == "published";
            var result = entry.Menu is not null && (!failed || stale) ? entry.Menu : Empty(entry.Status);
            return result with { LastCheckedAtUtc = entry.CheckedAt, IsStale = stale, SourceStatus = entry.Status };
        }
        finally { entry.Gate.Release(); }
    }

    public void Dispose()
    {
        rosenfeldt.Dispose();
        musikugglan.Dispose();
    }
}
