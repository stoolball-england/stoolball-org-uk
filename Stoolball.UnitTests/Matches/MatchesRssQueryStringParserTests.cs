using System;
using System.Linq;
using Stoolball.Matches;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class MatchesRssQueryStringParserTests
    {
        [Fact]
        public void No_date_in_querystring_returns_default_date_filter_as_UK_date()
        {
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString(string.Empty);

            var ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById(Constants.UkTimeZone());
            var ukToday = new DateTimeOffset(DateTimeOffset.UtcNow.Date, ukTimeZone.GetUtcOffset(DateTimeOffset.UtcNow.Date));

            Assert.Equal(ukToday, result.FromDate);
            Assert.Equal(ukToday.AddDays(366).AddSeconds(-1), result.UntilDate);
        }

        [Fact]
        public void Today_filter_is_parsed_correctly_as_UK_date()
        {
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString("?today=true");

            var ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById(Constants.UkTimeZone());
            var ukToday = new DateTimeOffset(DateTimeOffset.UtcNow.Date, ukTimeZone.GetUtcOffset(DateTimeOffset.UtcNow.Date));

            Assert.Equal(ukToday, result.FromDate);
            Assert.Equal(ukToday.AddDays(1).AddSeconds(-1), result.UntilDate);
        }

        [Theory]
        [InlineData("?from=2051-02-21", 2051, 2, 21, 0, null, null, null, null)] // far future because dates earlier than today are discarded
        [InlineData("?to=2021-02-21", null, null, null, null, 2021, 2, 21, 0)]
        [InlineData("?from=2050-07-01&to=2050-07-31", 2050, 7, 1, 1, 2050, 7, 31, 1)]
        public void Date_filters_are_parsed_correctly_as_UK_dates(string queryString, int? fromYear, int? fromMonth, int? fromDate, int? fromOffsetHours, int? untilYear, int? untilMonth, int? untilDate, int? untilOffsetHours)
        {
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString(queryString);

            if (fromYear.HasValue && fromMonth.HasValue && fromDate.HasValue && fromOffsetHours.HasValue)
            {
                Assert.Equal(new DateTimeOffset(fromYear.Value, fromMonth.Value, fromDate.Value, 0, 0, 0, TimeSpan.FromHours(fromOffsetHours.Value)), result.FromDate);
            }

            if (untilYear.HasValue && untilMonth.HasValue && untilDate.HasValue && untilOffsetHours.HasValue)
            {
                Assert.Equal(new DateTimeOffset(untilYear.Value, untilMonth.Value, untilDate.Value, 0, 0, 0, TimeSpan.FromHours(untilOffsetHours.Value)).AddDays(1).AddSeconds(-1), result.UntilDate);
            }
        }

        [Theory]
        [InlineData("", null)]
        [InlineData("?player=1", PlayerType.Mixed)]
        [InlineData("?player=2", PlayerType.Ladies)]
        public void PlayerType_filter_is_parsed_correctly(string queryString, PlayerType? expectedPlayerType)
        {
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString(queryString);

            if (expectedPlayerType.HasValue)
            {
                Assert.Single(result.PlayerTypes);
                Assert.Equal(expectedPlayerType, result.PlayerTypes.First());
            }
            else
            {
                Assert.Empty(result.PlayerTypes);
            }
        }

        [Theory]
        [InlineData("", null)]
        [InlineData("?type=0", MatchType.LeagueMatch)]
        [InlineData("?type=3", MatchType.TrainingSession)]
        [InlineData("?type=4", MatchType.FriendlyMatch)]
        [InlineData("?type=5", MatchType.KnockoutMatch)]
        public void MatchType_filter_is_parsed_correctly(string queryString, MatchType? expectedMatchType)
        {
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString(queryString);

            if (expectedMatchType.HasValue)
            {
                Assert.Single(result.MatchTypes);
                Assert.Equal(expectedMatchType, result.MatchTypes.First());
                Assert.True(result.IncludeMatches);
                Assert.False(result.IncludeTournaments);
                Assert.False(result.IncludeTournamentMatches);
            }
            else
            {
                Assert.Empty(result.MatchTypes);
                Assert.True(result.IncludeMatches);
                Assert.False(result.IncludeTournaments);
                Assert.False(result.IncludeTournamentMatches);
            }
        }

        [Fact]
        public void Tournament_filter_is_parsed_correctly()
        {
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString("?type=1");

            Assert.False(result.IncludeMatches);
            Assert.True(result.IncludeTournaments);
            Assert.False(result.IncludeTournamentMatches);
        }


        [Fact]
        public void No_querystring_returns_no_match_result_filter()
        {
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString(string.Empty);

            Assert.Empty(result.MatchResultTypes);
        }
    }
}
