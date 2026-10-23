using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace BigBrain.Api.MealPlanner;

public sealed record SchoolLunchDay(DateOnly Date, string Status, IReadOnlyList<string> Dishes);
public sealed record SchoolMealWeek(string SchoolId, string SchoolName, string SourceName, string SourceUrl,
    int Year, int Week, string Status, string DateBasis, DateTimeOffset? FetchedAtUtc, bool IsFixture,
    IReadOnlyList<SchoolLunchDay> Days);

// No HTTP client, storage dependency or scheduler: external access is deliberately not implemented.
public static class SchoolMeals
{
    public const string FacilityId = "C9643AEF-D05B-463E-84CB-76BEB47FF685";
    public const string Rosenfeldt = "rosenfeldt";
    public const string Musikugglan = "musikugglan";
    public const int MaxXmlCharacters = 524288;
    public static bool ValidWeek(int year, int week) => year is >= 1 and <= 9998 && week >= 1 && week <= ISOWeek.GetWeeksInYear(year);
    public static bool KnownSchool(string id) => id is Rosenfeldt or Musikugglan;

    public static SchoolMealWeek Unavailable(string id, int year, int week, bool fixture = false) =>
        Create(id, year, week, "offline", "unknown", fixture, new Dictionary<DateOnly, IReadOnlyList<string>>());

    public static SchoolMealWeek Preview(string id, int year, int week) => id == Rosenfeldt
        ? ParseRss(ExampleRss, year, week, true)
        : year == 2026 ? MusikugglanSchoolMeals.Parse(MusikugglanSchoolMeals.ExampleHtml, year, week, true)
        : Create(id, year, week, "notPublished", "unknown", true, new Dictionary<DateOnly, IReadOnlyList<string>>());

    public static SchoolMealWeek ParseRss(string xml, int year, int week, bool fixture = false)
    {
        if (!ValidWeek(year, week)) throw new ArgumentOutOfRangeException(nameof(week));
        SchoolMealWeek Invalid() => Create(Rosenfeldt, year, week, "invalid", "unknown", fixture, new Dictionary<DateOnly, IReadOnlyList<string>>());
        if (xml.Length > MaxXmlCharacters) return Invalid();
        try
        {
            using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaxXmlCharacters
            });
            var root = XDocument.Load(reader).Root;
            if (root?.Name != "rss" || (string?)root.Attribute("version") != "2.0") return Invalid();
            var channels = root.Elements("channel").ToArray();
            if (channels.Length != 1 || (string?)channels[0].Element("title") != "Skolmat - Rosenfeldtskolan") return Invalid();
            var days = new Dictionary<DateOnly, IReadOnlyList<string>>();
            var outsideWeek = false;
            foreach (var item in channels[0].Elements("item"))
            {
                foreach (var field in new[] { "title", "link", "guid", "description" })
                    if (item.Elements(field).Count() != 1 || string.IsNullOrWhiteSpace(item.Element(field)!.Value)) return Invalid();
                var guid = item.Element("guid")!.Value;
                var prefix = $"skolmat:menu:{FacilityId}:";
                if (!guid.StartsWith(prefix, StringComparison.Ordinal) ||
                    !DateOnly.TryParseExact(guid[prefix.Length..], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return Invalid();
                var dateTime = date.ToDateTime(TimeOnly.MinValue);
                outsideWeek |= ISOWeek.GetYear(dateTime) != year || ISOWeek.GetWeekOfYear(dateTime) != week;
                // XML has already decoded one layer. Split markup before decoding text entities.
                var dishes = Regex.Split(item.Element("description")!.Value, @"<br\s*/?>", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100))
                    .Select(text => WebUtility.HtmlDecode(text).Trim()).ToArray();
                if (dishes.Any(string.IsNullOrWhiteSpace) || !days.TryAdd(date, dishes)) return Invalid();
            }
            if (outsideWeek) return Create(Rosenfeldt, year, week, "notPublished", "unknown", fixture, new Dictionary<DateOnly, IReadOnlyList<string>>());
            return Create(Rosenfeldt, year, week, days.Count == 0 ? "notPublished" : "published", "sourceGuid", fixture, days);
        }
        catch (Exception exception) when (exception is XmlException or RegexMatchTimeoutException)
        {
            return Invalid();
        }
    }

    internal static SchoolMealWeek Create(string id, int year, int week, string status, string basis, bool fixture,
        Dictionary<DateOnly, IReadOnlyList<string>> dishes)
    {
        if (!KnownSchool(id) || !ValidWeek(year, week)) throw new ArgumentException("Invalid school or ISO week.");
        var monday = DateOnly.FromDateTime(ISOWeek.ToDateTime(year, week, DayOfWeek.Monday));
        var days = Enumerable.Range(0, 5).Select(offset =>
        {
            var date = monday.AddDays(offset);
            return dishes.TryGetValue(date, out var values)
                ? new SchoolLunchDay(date, "published", values)
                : new SchoolLunchDay(date, status == "published" ? "missing" : status, []);
        }).ToArray();
        return new(id, id == Rosenfeldt ? "Rosenfeldtskolan" : "Musikugglan",
            id == Rosenfeldt ? "Skolmat.info" : "Tant Grön",
            id == Rosenfeldt ? $"https://www.skolmat.info/matsedlar?facilityId={FacilityId}" : "https://www.tantgron.nu/matsedel/",
            year, week, status, basis, null, fixture, days);
    }

    // Owner-supplied single real item; missing days intentionally remain missing. Never fetched/live.
    public const string ExampleRss = """
        <rss version="2.0"><channel><title>Skolmat - Rosenfeldtskolan</title><ttl>5</ttl>
        <item><title>Måndag 19 oktober 2026</title>
        <link>https://www.skolmat.info/matsedlar?facilityId=C9643AEF-D05B-463E-84CB-76BEB47FF685&amp;year=2026&amp;week=43</link>
        <guid isPermaLink="false">skolmat:menu:C9643AEF-D05B-463E-84CB-76BEB47FF685:2026-10-19</guid>
        <description>Pasta med spenat och ostsås&lt;br /&gt;Pasta serveras med ost &amp;amp; skinksås</description>
        </item></channel></rss>
        """;
}
