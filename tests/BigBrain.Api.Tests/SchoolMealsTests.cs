using BigBrain.Api.MealPlanner;

namespace BigBrain.Api.Tests;

public sealed class SchoolMealsTests
{
    [Fact]
    public void OwnerExampleDecodesEntitiesAndKeepsMissingDays()
    {
        var menu = SchoolMeals.Preview(SchoolMeals.Rosenfeldt, 2026, 43);
        Assert.Equal("published", menu.Status);
        Assert.True(menu.IsFixture);
        Assert.Null(menu.FetchedAtUtc);
        Assert.Equal("sourceGuid", menu.DateBasis);
        Assert.Equal(new DateOnly(2026, 10, 19), menu.Days[0].Date);
        Assert.Equal(["Pasta med spenat och ostsås", "Pasta serveras med ost & skinksås"], menu.Days[0].Dishes);
        Assert.Equal(5, menu.Days.Count);
        Assert.All(menu.Days.Skip(1), day => { Assert.Equal("missing", day.Status); Assert.Empty(day.Dishes); });
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
        var xml = SchoolMeals.ExampleRss.Replace("2026-10-19", "2024-12-30", StringComparison.Ordinal);
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
    public void UnverifiedHtmlHasNoInventedMenuOrYearProvenance()
    {
        var menu = SchoolMeals.Preview(SchoolMeals.Musikugglan, 2026, 43);
        Assert.Equal("sourceEvidenceMissing", menu.Status);
        Assert.Equal("unknown", menu.DateBasis);
        Assert.All(menu.Days, day => Assert.Empty(day.Dishes));
        Assert.Equal("offline", SchoolMeals.Unavailable(SchoolMeals.Rosenfeldt, 2026, 43).Status);
    }
}
