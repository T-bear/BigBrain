using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace BigBrain.Api.MealPlanner;

public sealed record SchoolLunchDay(DateOnly Date, string Status, IReadOnlyList<string> Dishes);
public sealed record SchoolMealWeek(string SchoolId, string SchoolName, string SourceName, string SourceUrl,
    int Year, int Week, string Status, string DateBasis, DateTimeOffset? FetchedAtUtc, bool IsFixture,
    IReadOnlyList<SchoolLunchDay> Days)
{
    public DateTimeOffset? LastCheckedAtUtc { get; init; }
    public DateTimeOffset? SourceUpdatedAtUtc { get; init; }
    public bool IsStale { get; init; }
    public string? SourceStatus { get; init; }
}

// Pure parsing and offline fixtures; HTTP/cache ownership stays in SchoolMealService.
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

    // Historical offline fixture from the owner-provided original; never fetched/live.
    public const string ExampleRss = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom">
          <channel>
            <title>Skolmat - Rosenfeldtskolan</title>
            <link>https://www.skolmat.info/matsedlar?facilityId=C9643AEF-D05B-463E-84CB-76BEB47FF685</link>
            <description>Matsedel för Rosenfeldtskolan, Karlskrona.</description>
            <language>sv-SE</language>
            <generator>Skolmat</generator>
            <ttl>5</ttl>
            <atom:link href="https://www.skolmat.info/api/public/matsedlar/C9643AEF-D05B-463E-84CB-76BEB47FF685/rss?period=current-week" rel="self" type="application/rss+xml" />
            <item>
              <title>Måndag 19 oktober 2026</title>
              <link>https://www.skolmat.info/matsedlar?facilityId=C9643AEF-D05B-463E-84CB-76BEB47FF685&amp;year=2026&amp;week=43</link>
              <guid isPermaLink="false">skolmat:menu:C9643AEF-D05B-463E-84CB-76BEB47FF685:2026-10-19</guid>
              <description>Pasta med spenat och ostsås&lt;br /&gt;Pasta serveras med ost &amp;amp; skinksås</description>
            </item>
            <item>
              <title>Tisdag 20 oktober 2026</title>
              <link>https://www.skolmat.info/matsedlar?facilityId=C9643AEF-D05B-463E-84CB-76BEB47FF685&amp;year=2026&amp;week=43</link>
              <guid isPermaLink="false">skolmat:menu:C9643AEF-D05B-463E-84CB-76BEB47FF685:2026-10-20</guid>
              <description>Grönsakragu serveras med kokt potatis&lt;br /&gt;Krämigt örtbakad fisk serveras med kokt potatis</description>
            </item>
            <item>
              <title>Onsdag 21 oktober 2026</title>
              <link>https://www.skolmat.info/matsedlar?facilityId=C9643AEF-D05B-463E-84CB-76BEB47FF685&amp;year=2026&amp;week=43</link>
              <guid isPermaLink="false">skolmat:menu:C9643AEF-D05B-463E-84CB-76BEB47FF685:2026-10-21</guid>
              <description>Korv serveras med potatismos, ketchup &amp;amp; senap&lt;br /&gt;Stekt kycklingkorv serveras med potatismos, ketchup och senap</description>
            </item>
            <item>
              <title>Torsdag 22 oktober 2026</title>
              <link>https://www.skolmat.info/matsedlar?facilityId=C9643AEF-D05B-463E-84CB-76BEB47FF685&amp;year=2026&amp;week=43</link>
              <guid isPermaLink="false">skolmat:menu:C9643AEF-D05B-463E-84CB-76BEB47FF685:2026-10-22</guid>
              <description>Gulaschsoppa serveras med bröd och ost&lt;br /&gt;Gulaschsoppa serveras med bröd och ost</description>
            </item>
            <item>
              <title>Fredag 23 oktober 2026</title>
              <link>https://www.skolmat.info/matsedlar?facilityId=C9643AEF-D05B-463E-84CB-76BEB47FF685&amp;year=2026&amp;week=43</link>
              <guid isPermaLink="false">skolmat:menu:C9643AEF-D05B-463E-84CB-76BEB47FF685:2026-10-23</guid>
              <description>Kockens val&lt;br /&gt;Kockens val</description>
            </item>
          </channel>
        </rss>
        """;
}
