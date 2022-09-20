using System;
using Stoolball.Matches;
using Xunit;

namespace Stoolball.UnitTests.Matches
{
    public class MatchFilterQueryStringParserTests
    {
        [Fact]
        public void Null_filter_throws_ArgumentNullException()
        {
            var parser = new MatchFilterQueryStringParser();

            Assert.Throws<ArgumentNullException>(() => _ = parser.ParseQueryString(null, string.Empty));
        }

        [Fact]
        public void Null_querystring_returns_cloned_filter()
        {
            var parser = new MatchFilterQueryStringParser();
            var filter = new MatchFilter();

            var result = parser.ParseQueryString(filter, null);

            Assert.NotEqual(filter, result);
            Assert.Equal(filter.CompetitionIds.Count, result.CompetitionIds.Count);
            Assert.Equal(filter.FromDate, result.FromDate);
            Assert.Equal(filter.IncludeMatches, result.IncludeMatches);
            Assert.Equal(filter.IncludeTournamentMatches, result.IncludeTournamentMatches);
            Assert.Equal(filter.IncludeTournaments, result.IncludeTournaments);
            Assert.Equal(filter.MatchLocationIds.Count, result.MatchLocationIds.Count);
            Assert.Equal(filter.MatchResultTypes.Count, result.MatchResultTypes.Count);
            Assert.Equal(filter.MatchTypes.Count, result.MatchTypes.Count);
            Assert.Equal(filter.Paging.PageSize, result.Paging.PageSize);
            Assert.Equal(filter.Paging.PageNumber, result.Paging.PageNumber);
            Assert.Equal(filter.Paging.Total, result.Paging.Total);
            Assert.Equal(filter.Paging.PageUrl, result.Paging.PageUrl);
            Assert.Equal(filter.PlayerTypes.Count, result.PlayerTypes.Count);
            Assert.Equal(filter.Query, result.Query);
            Assert.Equal(filter.SeasonIds.Count, result.SeasonIds.Count);
            Assert.Equal(filter.TeamIds.Count, result.TeamIds.Count);
            Assert.Equal(filter.TournamentId, result.TournamentId);
            Assert.Equal(filter.UntilDate, result.UntilDate);
        }

        [Theory]
        [InlineData("?to=2021-12-31")]
        [InlineData("?from=")]
        [InlineData("?from=2021-02-31")]
        [InlineData("?from=invalid")]
        public void Missing_empty_or_invalid_FromDate_is_null(string queryString)
        {
            var parser = new MatchFilterQueryStringParser();

            var filter = parser.ParseQueryString(new MatchFilter(), queryString);

            Assert.Null(filter.FromDate);
        }

        [Fact]
        public void FromDate_is_parsed()
        {
            var parser = new MatchFilterQueryStringParser();

            var filter = parser.ParseQueryString(new MatchFilter(), "?from=2020-07-10");

            Assert.NotNull(filter.FromDate);
            Assert.Equal(new DateTime(2020, 7, 10), filter.FromDate!.Value.Date);
        }

        [Theory]
        [InlineData("?from=2021-12-31")]
        [InlineData("?to=")]
        [InlineData("?to=2021-02-31")]
        [InlineData("?to=invalid")]
        public void Missing_empty_or_invalid_UntilDate_is_null(string queryString)
        {
            var parser = new MatchFilterQueryStringParser();

            var filter = parser.ParseQueryString(new MatchFilter(), queryString);

            Assert.Null(filter.UntilDate);
        }

        [Fact]
        public void UntilDate_is_parsed()
        {
            var parser = new MatchFilterQueryStringParser();

            var filter = parser.ParseQueryString(new MatchFilter(), "to=2022-06-09");

            Assert.NotNull(filter.UntilDate);
            Assert.Equal(new DateTime(2022, 6, 9), filter.UntilDate!.Value.Date);
        }

        [Fact]
        public void Query_is_parsed_and_trimmed()
        {
            var parser = new MatchFilterQueryStringParser();

            var filter = parser.ParseQueryString(new MatchFilter(), "q=%20hello%20world%20");

            Assert.Equal("hello world", filter.Query);
        }
    }
}
