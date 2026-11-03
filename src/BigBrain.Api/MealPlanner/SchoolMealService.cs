using System.Globalization;

namespace BigBrain.Api.MealPlanner;

public sealed record SchoolMealOptions
{
    public bool Enabled { get; init; }
    public bool RosenfeldtEnabled { get; init; }
    public bool MusikugglanEnabled { get; init; }
}

// One singleton per API process: independent, on-demand memory caches and source cooldowns.
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
        public DateTimeOffset? FreshUntil;
        public SchoolMealCachePolicy? CachePolicy;
        public void ClearMenu()
        {
            Menu = null;
            ValidatedAt = null;
            ETag = null;
            Modified = null;
            FreshUntil = null;
            CachePolicy = null;
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
                // Unknown response headers may contain a longer Retry-After. Keep this school
                // blocked in memory unless the client can establish a safe completed response.
                entry.NextAttempt = DateTimeOffset.MaxValue;
                entry.CheckedAt = now;
                var response = await source.GetAsync(school, entry.ETag, entry.Modified, token, entry.CachePolicy);
                var completedAt = clock.GetUtcNow();
                var next = completedAt.AddHours(6);
                if (response.RetryAt > next) next = response.RetryAt.Value;
                entry.NextAttempt = response.HeadersVerified ? next : DateTimeOffset.MaxValue;
                if (!response.HeadersVerified)
                {
                    if (response.Status != "cachePolicy") response = response with { Status = "unavailable", Content = null };
                }
                entry.Status = response.Status;
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
                        entry.FreshUntil = response.FreshUntil;
                        entry.CachePolicy = response.CachePolicy;
                        entry.ETag = response.ETag;
                        entry.Modified = response.Modified;
                        entry.Menu = entry.Menu with { SourceUpdatedAtUtc = response.Modified };
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
                        entry.FreshUntil = response.FreshUntil;
                        entry.CachePolicy = response.CachePolicy;
                    }
                }
            }
            if (CurrentWeek() != (year, week)) return Empty("notPublished");
            var failed = entry.Status is "invalid" or "unavailable" or "cachePolicy";
            var fresh = entry.Menu is not null && clock.GetUtcNow() < entry.FreshUntil;
            var result = fresh && !failed ? entry.Menu! : Empty(failed || entry.Menu is null ? entry.Status : "unavailable");
            return result with
            {
                LastCheckedAtUtc = entry.CheckedAt,
                IsStale = false,
                SourceStatus = result.Status,
                ValidUntilUtc = fresh && !failed ? entry.FreshUntil : null
            };
        }
        finally { entry.Gate.Release(); }
    }

    public void Dispose()
    {
        rosenfeldt.Dispose();
        musikugglan.Dispose();
    }
}
