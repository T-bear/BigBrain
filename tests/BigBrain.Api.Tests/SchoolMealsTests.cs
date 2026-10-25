using BigBrain.Api.MealPlanner;
using System.Xml.Linq;

namespace BigBrain.Api.Tests;

public sealed class SchoolMealsTests
{
    private static string CompactHtml => System.Text.RegularExpressions.Regex.Replace(
        MusikugglanSchoolMeals.ExampleHtml.Replace("\u00a0", "", StringComparison.Ordinal), @">\s+<", "><");
    [Fact]
    public void OriginalRssDecodesEntitiesAndPreservesAllFiveDays()
    {
        var menu = SchoolMeals.Preview(SchoolMeals.Rosenfeldt, 2026, 43);
        Assert.Equal("published", menu.Status);
        Assert.True(menu.IsFixture);
        Assert.Null(menu.FetchedAtUtc);
        Assert.Equal("sourceGuid", menu.DateBasis);
        Assert.Equal(new DateOnly(2026, 10, 19), menu.Days[0].Date);
        Assert.Equal(["Pasta med spenat och ostsås", "Pasta serveras med ost & skinksås"], menu.Days[0].Dishes);
        Assert.Equal(5, menu.Days.Count);
        Assert.All(menu.Days, day => { Assert.Equal("published", day.Status); Assert.Equal(2, day.Dishes.Count); });
    }

    [Fact]
    public void DuplicateAlternativesAreNotDeduplicated()
    {
        var xml = SchoolMeals.ExampleRss.Replace("Pasta serveras med ost &amp;amp; skinksås", "Pasta med spenat och ostsås", StringComparison.Ordinal);
        var result = SchoolMeals.ParseRss(xml, 2026, 43);
        Assert.Equal(2, result.Days[0].Dishes.Count);
        Assert.Equal(result.Days[0].Dishes[0], result.Days[0].Dishes[1]);
    }

    [Theory]
    [InlineData("<rss>")]
    [InlineData("<!DOCTYPE rss [<!ENTITY x SYSTEM 'file:///never-read'>]><rss version='2.0'><channel><title>&x;</title></channel></rss>")]
    [InlineData("<rss version='2.0'><channel/></rss>")]
    public void InvalidXmlAndExternalEntitiesAreRejected(string xml) => Assert.Equal("invalid", SchoolMeals.ParseRss(xml, 2026, 43).Status);

    [Theory]
    [InlineData("2026-10-19", "2026-02-30")]
    [InlineData("<description>", "<missing>")]
    [InlineData("Rosenfeldtskolan", "Other school")]
    [InlineData("C9643AEF-D05B-463E-84CB-76BEB47FF685", "OTHER")]
    public void MalformedIdentityDateOrRequiredFieldFailsClosed(string before, string after) =>
        Assert.Equal("invalid", SchoolMeals.ParseRss(SchoolMeals.ExampleRss.Replace(before, after, StringComparison.Ordinal), 2026, 43).Status);

    [Fact]
    public void WrongWeekNeverReusesFixture()
    {
        var menu = SchoolMeals.Preview(SchoolMeals.Rosenfeldt, 2026, 44);
        Assert.Equal("notPublished", menu.Status);
        Assert.All(menu.Days, day => Assert.Empty(day.Dishes));
        Assert.False(SchoolMeals.ValidWeek(2025, 53));
        Assert.False(SchoolMeals.ValidWeek(0, 1));
        Assert.False(SchoolMeals.ValidWeek(2026, 0));
    }

    [Fact]
    public void IsoYearCanDifferFromCalendarYear()
    {
        var document = XDocument.Parse(SchoolMeals.ExampleRss);
        document.Descendants("item").Skip(1).Remove();
        var xml = document.ToString().Replace("2026-10-19", "2024-12-30", StringComparison.Ordinal);
        var menu = SchoolMeals.ParseRss(xml, 2025, 1);
        Assert.Equal("published", menu.Status);
        Assert.Equal(new DateOnly(2024, 12, 30), menu.Days[0].Date);
    }

    [Fact]
    public void OversizeAndDuplicateDatesAreInvalid()
    {
        Assert.Equal("invalid", SchoolMeals.ParseRss(new string('x', SchoolMeals.MaxXmlCharacters + 1), 2026, 43).Status);
        var xml = SchoolMeals.ExampleRss;
        var item = xml[xml.IndexOf("<item>", StringComparison.Ordinal)..(xml.IndexOf("</item>", StringComparison.Ordinal) + 7)];
        Assert.Equal("invalid", SchoolMeals.ParseRss(xml.Replace("</channel>", item + "</channel>", StringComparison.Ordinal), 2026, 43).Status);
    }

    [Fact]
    public void OriginalRssPreservesEveryDishAndGuidDate()
    {
        string[][] expected = [
            ["Pasta med spenat och ostsås", "Pasta serveras med ost & skinksås"],
            ["Grönsakragu serveras med kokt potatis", "Krämigt örtbakad fisk serveras med kokt potatis"],
            ["Korv serveras med potatismos, ketchup & senap", "Stekt kycklingkorv serveras med potatismos, ketchup och senap"],
            ["Gulaschsoppa serveras med bröd och ost", "Gulaschsoppa serveras med bröd och ost"],
            ["Kockens val", "Kockens val"]
        ];
        var result = SchoolMeals.ParseRss(SchoolMeals.ExampleRss, 2026, 43, true);
        for (var index = 0; index < 5; index++)
        {
            Assert.Equal(new DateOnly(2026, 10, 19 + index), result.Days[index].Date);
            Assert.Equal(expected[index], result.Days[index].Dishes);
        }
        var document = XDocument.Parse(SchoolMeals.ExampleRss);
        document.Descendants("item").Skip(1).Remove();
        var missing = SchoolMeals.ParseRss(document.ToString(), 2026, 43);
        Assert.All(missing.Days.Skip(1), day => Assert.Equal("missing", day.Status));
    }

    [Fact]
    public void OriginalHtmlPreservesFiveLunchTextsAndExcludesSnacks()
    {
        var result = MusikugglanSchoolMeals.Parse(MusikugglanSchoolMeals.ExampleHtml, 2026, 44, true);
        Assert.Equal("published", result.Status);
        Assert.Equal("htmlWeekRequestedYear", result.DateBasis);
        Assert.Null(result.FetchedAtUtc);
        Assert.True(result.IsFixture);
        Assert.All(result.Days, day => { Assert.Equal("published", day.Status); Assert.Single(day.Dishes); });
        Assert.Equal("Pasta med köttfärssås/Vegosås", result.Days[0].Dishes[0]);
        Assert.Equal("Currygryta med kyckling samt ris/Vegogryta", result.Days[1].Dishes[0]);
        Assert.Equal("Stekt falukorv med makaroner och ketchup/Vegokorv", result.Days[2].Dishes[0]);
        Assert.Equal("Ugnspannkaka med baconströssel och stekt potatis,lingonsylt", result.Days[3].Dishes[0]);
        Assert.Equal("Fläsknoisette och stekt kulpotatis samt kall sås /Vegobiff", result.Days[4].Dishes[0]);
        Assert.DoesNotContain(result.Days.SelectMany(day => day.Dishes), dish => dish.Contains("mellanmål", StringComparison.Ordinal));
    }

    [Fact]
    public void HtmlWhitespaceAndEntitiesAreTextAndSlashIsNotASplit()
    {
        var html = CompactHtml.Replace("Pasta med köttfärssås/Vegosås", "  Syntetiskt&nbsp; &amp;\n test<br>fortsättning / alternativ &lt;tagg&gt;", StringComparison.Ordinal);
        var result = MusikugglanSchoolMeals.Parse(html, 2026, 44);
        Assert.Equal("Syntetiskt & test fortsättning / alternativ <tagg>", Assert.Single(result.Days[0].Dishes));
    }

    [Theory]
    [InlineData("<tr><td>Måndag</td><td><p>Pasta med köttfärssås/Vegosås</p></td></tr>", "")]
    [InlineData("<p>Pasta med köttfärssås/Vegosås</p>", "")]
    public void HtmlMissingDayIsNotAHoliday(string before, string after)
    {
        var result = MusikugglanSchoolMeals.Parse(CompactHtml.Replace(before, after, StringComparison.Ordinal), 2026, 44);
        Assert.Equal("published", result.Status);
        Assert.Equal("missing", result.Days[0].Status);
        Assert.Empty(result.Days[0].Dishes);
    }

    [Theory]
    [InlineData("</table>", "</table><p>Unexpected content</p>")]
    [InlineData("Matsedel v. 44", "Ny rubrik")]
    [InlineData("Matsedel v. 44", "Matsedel v. ٤٤")]
    [InlineData("Mellanmål v. 44", "Mellanmål v. 43")]
    [InlineData("</table>", "")]
    [InlineData("<td>Tisdag</td>", "<td>Måndag</td>")]
    [InlineData("<td>Tisdag</td>", "<td>Okänd dag</td>")]
    [InlineData("<td>Tisdag</td>", "<th>Tisdag</th>")]
    [InlineData("<p>Pasta med köttfärssås/Vegosås</p>", "<iframe>Otillåten struktur</iframe>")]
    public void ChangedOrBrokenHtmlFailsClosed(string before, string after) =>
        Assert.Equal("invalid", MusikugglanSchoolMeals.Parse(CompactHtml.Replace(before, after, StringComparison.Ordinal), 2026, 44).Status);

    [Fact]
    public void HtmlWrongWeekAndUnknownYearRemainExplicit()
    {
        var wrongWeek = SchoolMeals.Preview(SchoolMeals.Musikugglan, 2026, 43);
        Assert.Equal("notPublished", wrongWeek.Status);
        Assert.All(wrongWeek.Days, day => Assert.Empty(day.Dishes));
        Assert.Equal("notPublished", SchoolMeals.Preview(SchoolMeals.Musikugglan, 2025, 44).Status);
        var derived = MusikugglanSchoolMeals.Parse(MusikugglanSchoolMeals.ExampleHtml, 2025, 44);
        Assert.Equal("htmlWeekRequestedYear", derived.DateBasis);
        Assert.Equal(2025, derived.Days[0].Date.Year);
        Assert.Equal("offline", SchoolMeals.Unavailable(SchoolMeals.Musikugglan, 2026, 44).Status);
    }

    [Fact]
    public void HtmlRejectsOversizeAmbiguousAndDeepContent()
    {
        Assert.Equal("invalid", MusikugglanSchoolMeals.Parse(new string('x', SchoolMeals.MaxXmlCharacters + 1), 2026, 44).Status);
        Assert.Equal("invalid", MusikugglanSchoolMeals.Parse(MusikugglanSchoolMeals.ExampleHtml + MusikugglanSchoolMeals.ExampleHtml, 2026, 44).Status);
        var deep = CompactHtml.Replace("Pasta med köttfärssås/Vegosås", string.Concat(Enumerable.Repeat("<span>", 20)) + "test" + string.Concat(Enumerable.Repeat("</span>", 20)), StringComparison.Ordinal);
        Assert.Equal("invalid", MusikugglanSchoolMeals.Parse(deep, 2026, 44).Status);
    }
}
