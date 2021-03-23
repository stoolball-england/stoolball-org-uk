using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Stoolball.Matches;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class MatchesRssQueryStringParserTests
    {
        [Fact]
        public void No_date_in_querystring_returns_default_date_filter()
        {
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString(new NameValueCollection());

            Assert.Equal(DateTimeOffset.UtcNow.Date.AddDays(-1), result.FromDate);
            Assert.Equal(DateTimeOffset.UtcNow.Date.AddDays(366), result.UntilDate);
        }

        [Fact]
        public void Today_filter_is_parsed_correctly()
        {
            var parsedQueryString = HttpUtility.ParseQueryString("?format=tweet&today=true");
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString(parsedQueryString);

            Assert.Equal(DateTimeOffset.UtcNow.Date, result.FromDate);
            Assert.Equal(DateTimeOffset.UtcNow.Date.AddDays(1), result.UntilDate);
        }

        [Theory]
        [InlineData("?from=2021-02-21", 2021, 2, 21, 0, null, null, null, null)]
        [InlineData("?to=2021-02-21", null, null, null, null, 2021, 2, 21, 0)]
        [InlineData("?from=2020-07-01&to=2020-07-31", 2020, 7, 1, 1, 2020, 7, 31, 1)]
        public void Date_filters_are_parsed_correctly(string queryString, int? fromYear, int? fromMonth, int? fromDate, int? fromOffsetHours, int? untilYear, int? untilMonth, int? untilDate, int? untilOffsetHours)
        {
            var parsedQueryString = HttpUtility.ParseQueryString(queryString);
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString(parsedQueryString);

            if (fromYear.HasValue && fromMonth.HasValue && fromDate.HasValue && fromOffsetHours.HasValue)
            {
                Assert.Equal(new DateTimeOffset(fromYear.Value, fromMonth.Value, fromDate.Value, 0, 0, 0, TimeSpan.FromHours(fromOffsetHours.Value)), result.FromDate);
            }

            if (untilYear.HasValue && untilMonth.HasValue && untilDate.HasValue && untilOffsetHours.HasValue)
            {
                Assert.Equal(new DateTimeOffset(untilYear.Value, untilMonth.Value, untilDate.Value, 0, 0, 0, TimeSpan.FromHours(untilOffsetHours.Value)).AddDays(1), result.UntilDate);
            }
        }

        [Theory]
        [InlineData("", null)]
        [InlineData("?player=1", PlayerType.Mixed)]
        [InlineData("?player=2", PlayerType.Ladies)]
        public void PlayerType_filter_is_parsed_correctly(string queryString, PlayerType? expectedPlayerType)
        {
            var parsedQueryString = HttpUtility.ParseQueryString(queryString);
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString(parsedQueryString);

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
        [InlineData("?type=3", MatchType.Practice)]
        [InlineData("?type=4", MatchType.FriendlyMatch)]
        [InlineData("?type=5", MatchType.KnockoutMatch)]
        public void MatchType_filter_is_parsed_correctly(string queryString, MatchType? expectedMatchType)
        {
            var parsedQueryString = HttpUtility.ParseQueryString(queryString);
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString(parsedQueryString);

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
                Assert.True(result.IncludeTournaments);
                Assert.False(result.IncludeTournamentMatches);
            }
        }

        [Fact]
        public void Tournament_filter_is_parsed_correctly()
        {
            var parsedQueryString = HttpUtility.ParseQueryString("?type=1");
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString(parsedQueryString);

            Assert.False(result.IncludeMatches);
            Assert.True(result.IncludeTournaments);
            Assert.False(result.IncludeTournamentMatches);
        }


        [Fact]
        public void No_tweet_format_returns_no_match_result_filter()
        {
            var parsedQueryString = HttpUtility.ParseQueryString(string.Empty);
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString(parsedQueryString);

            Assert.Empty(result.MatchResultTypes);
        }

        [Fact]
        public void Tweets_should_only_return_matches_with_an_unknown_result()
        {
            var parsedQueryString = HttpUtility.ParseQueryString("?format=tweet");
            var parser = new MatchesRssQueryStringParser();

            var result = parser.ParseFilterFromQueryString(parsedQueryString);

            Assert.Single(result.MatchResultTypes);
            Assert.Null(result.MatchResultTypes.First());
        }
    }
}
