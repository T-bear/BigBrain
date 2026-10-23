using System.Globalization;
using System.Net;
using System.Security;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace BigBrain.Api.MealPlanner;

// Narrow parser for the owner-supplied heading/table/snack boundary, not a general HTML engine.
public static class MusikugglanSchoolMeals
{
    private static readonly string[] Weekdays = ["Måndag", "Tisdag", "Onsdag", "Torsdag", "Fredag"];
    private static readonly string[] AllowedTags = ["tbody", "tr", "td", "p", "br", "strong", "em", "span", "b", "i"];
    private static readonly string[] InlineTags = ["p", "br", "strong", "em", "span", "b", "i"];
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);
    private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant;
    private const string HeadingStart = @"<p\b[^>]*>\s*<strong\b[^>]*>\s*";
    private const string HeadingEnd = @"\s*(?:<br\s*/?>\s*)?</strong>\s*</p>";

    public static SchoolMealWeek Parse(string html, int year, int week, bool fixture = false)
    {
        if (!SchoolMeals.ValidWeek(year, week)) throw new ArgumentOutOfRangeException(nameof(week));
        SchoolMealWeek Result(string status, string basis = "unknown", Dictionary<DateOnly, IReadOnlyList<string>>? days = null) =>
            SchoolMeals.Create(SchoolMeals.Musikugglan, year, week, status, basis, fixture, days ?? []);
        if (html.Length > SchoolMeals.MaxXmlCharacters) return Result("invalid");
        try
        {
            // Ignore non-rendered content before finding the unique, explicitly bounded lunch fragment.
            html = Regex.Replace(html, @"<!--.*?-->|<script\b[^>]*>.*?</script\s*>|<style\b[^>]*>.*?</style\s*>", "", Options, MatchTimeout);
            var headings = Regex.Matches(html, HeadingStart + @"Matsedel\s+v\.\s*(?<week>\d{1,2})" + HeadingEnd, Options, MatchTimeout);
            if (headings.Count != 1) return Result("invalid");
            var heading = headings[0];
            var sourceWeek = int.Parse(heading.Groups["week"].Value, CultureInfo.InvariantCulture);
            if (sourceWeek is < 1 or > 53) return Result("invalid");
            var remainder = html[(heading.Index + heading.Length)..];
            var snack = Regex.Match(remainder, HeadingStart + @"Mellanmål\s+v\.\s*(?<week>\d{1,2})" + HeadingEnd, Options, MatchTimeout);
            if (!snack.Success || int.Parse(snack.Groups["week"].Value, CultureInfo.InvariantCulture) != sourceWeek) return Result("invalid");
            var fragment = remainder[..snack.Index].Trim();
            if (!Regex.IsMatch(fragment, @"\A<table\b[^>]*>.*</table>\z", Options, MatchTimeout)) return Result("invalid");
            fragment = Regex.Replace(fragment, @"<br\s*/?>", "<br />", Options, MatchTimeout);
            // Translate HTML entities to XML-safe text without interpreting decoded text as markup.
            fragment = Regex.Replace(fragment, @"&(?:#[0-9]+|#x[0-9a-f]+|[a-z][a-z0-9]+);",
                match => SecurityElement.Escape(WebUtility.HtmlDecode(match.Value))!, Options, MatchTimeout);
            using var reader = XmlReader.Create(new StringReader(fragment), new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = SchoolMeals.MaxXmlCharacters
            });
            var table = XElement.Load(reader);
            if (table.Name != "table" || table.Descendants().Take(129).Count() > 128 || table.Descendants().Any(element => element.Ancestors().Take(13).Count() > 12)) return Result("invalid");
            var bodies = table.Elements("tbody").ToArray();
            var body = bodies.Length == 0 ? table : bodies.Length == 1 && table.Elements().Count() == 1 ? bodies[0] : null;
            if (body is null || body.Elements().Any(element => element.Name != "tr")) return Result("invalid");
            if (table.Descendants().Any(element => !AllowedTags.Contains(element.Name.LocalName, StringComparer.Ordinal) || element.Name.Namespace != XNamespace.None)) return Result("invalid");
            var days = new Dictionary<DateOnly, IReadOnlyList<string>>();
            var monday = DateOnly.FromDateTime(ISOWeek.ToDateTime(year, week, DayOfWeek.Monday));
            var previousIndex = -1;
            foreach (var row in body.Elements())
            {
                var cells = row.Elements().ToArray();
                if (cells.Length != 2 || cells.Any(cell => cell.Name != "td") || cells.Any(cell => cell.Descendants().Any(element => !InlineTags.Contains(element.Name.LocalName, StringComparer.Ordinal)))) return Result("invalid");
                var dayIndex = Array.IndexOf(Weekdays, Text(cells[0]));
                if (dayIndex <= previousIndex || dayIndex < 0) return Result("invalid");
                previousIndex = dayIndex;
                var dish = Text(cells[1]);
                if (dish.Length != 0) days.Add(monday.AddDays(dayIndex), new[] { dish });
            }
            if (sourceWeek != week) return Result("notPublished");
            return Result(days.Count == 0 ? "notPublished" : "published", "htmlWeekRequestedYear", days);
        }
        catch (Exception exception) when (exception is XmlException or RegexMatchTimeoutException)
        {
            return Result("invalid");
        }
    }

    private static string Text(XElement element) => Regex.Replace(Content(element), @"\s+", " ", RegexOptions.CultureInvariant, MatchTimeout).Trim();
    private static string Content(XElement element) => string.Concat(element.Nodes().Select(node => node switch
    {
        XText text => text.Value,
        XElement child when child.Name == "br" => " ",
        XElement child when child.Name == "p" => " " + Content(child) + " ",
        XElement child => Content(child),
        _ => ""
    }));

    // Normalized excerpt supplied in the owner task; not the original downloaded HTML file.
    public const string ExampleHtml = """
        <p><strong>Matsedel v. 44<br /></strong></p>
        <table border="2"><tbody>
        <tr><td>Måndag</td><td><p>Pasta med köttfärssås/Vegosås</p></td></tr>
        <tr><td>Tisdag</td><td>Currygryta med kyckling samt ris/Vegogryta</td></tr>
        <tr><td>Onsdag</td><td>Stekt falukorv med makaroner och ketchup/Vegokorv</td></tr>
        <tr><td>Torsdag</td><td>Ugnspannkaka med baconströssel och stekt potatis,lingonsylt</td></tr>
        <tr><td>Fredag</td><td>Fläsknoisette och stekt kulpotatis samt kall sås /Vegobiff</td></tr>
        </tbody></table>
        <p><strong>Mellanmål v. 44</strong></p>
        """;
}
