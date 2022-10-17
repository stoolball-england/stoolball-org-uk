using System;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.UnitTests.Statistics
{
    public class StatisticsFilterQueryStringParserTests
    {
        [Fact]
        public void Null_filter_throws_ArgumentNullException()
        {
            var parser = new StatisticsFilterQueryStringParser();

            Assert.Throws<ArgumentNullException>(() => _ = parser.ParseQueryString(null, string.Empty));
        }

        [Fact]
        public void Null_querystring_returns_cloned_filter()
        {
            var parser = new StatisticsFilterQueryStringParser();
            var filter = new StatisticsFilter();

            var result = parser.ParseQueryString(filter, null);

            Assert.NotEqual(filter, result);
            Assert.Equal(filter.BattingFirst, result.BattingFirst);
            Assert.Equal(filter.BattingPositions.Count, result.BattingPositions.Count);
            Assert.Equal(filter.BowledByPlayerIdentityIds.Count, result.BowledByPlayerIdentityIds.Count);
            Assert.Equal(filter.CaughtByPlayerIdentityIds.Count, result.CaughtByPlayerIdentityIds.Count);
            Assert.Equal(filter.Club?.ClubId, result.Club?.ClubId);
            Assert.Equal(filter.Competition?.CompetitionId, result.Competition?.CompetitionId);
            Assert.Equal(filter.DismissalTypes.Count, result.DismissalTypes.Count);
            Assert.Equal(filter.FromDate, result.FromDate);
            Assert.Equal(filter.MatchLocation?.MatchLocationId, result.MatchLocation?.MatchLocationId);
            Assert.Equal(filter.MatchTypes.Count, result.MatchTypes.Count);
            Assert.Equal(filter.MaxResultsAllowingExtraResultsIfValuesAreEqual, result.MaxResultsAllowingExtraResultsIfValuesAreEqual);
            Assert.Equal(filter.MinimumQualifyingInnings, result.MinimumQualifyingInnings);
            Assert.Equal(filter.OppositionTeamIds.Count, result.OppositionTeamIds.Count);
            Assert.Equal(filter.Paging.PageSize, result.Paging.PageSize);
            Assert.Equal(filter.Paging.PageNumber, result.Paging.PageNumber);
            Assert.Equal(filter.Paging.Total, result.Paging.Total);
            Assert.Equal(filter.Paging.PageUrl, result.Paging.PageUrl);
            Assert.Equal(filter.Player?.PlayerId, result.Player?.PlayerId);
            Assert.Equal(filter.PlayerOfTheMatch, result.PlayerOfTheMatch);
            Assert.Equal(filter.PlayerTypes.Count, result.PlayerTypes.Count);
            Assert.Equal(filter.RunOutByPlayerIdentityIds.Count, result.RunOutByPlayerIdentityIds.Count);
            Assert.Equal(filter.Season?.SeasonId, result.Season?.SeasonId);
            Assert.Equal(filter.SwapBattingFirstFilter, result.SwapBattingFirstFilter);
            Assert.Equal(filter.SwapTeamAndOppositionFilters, result.SwapTeamAndOppositionFilters);
            Assert.Equal(filter.Team?.TeamId, result.Team?.TeamId);
            Assert.Equal(filter.TournamentIds.Count, result.TournamentIds.Count);
            Assert.Equal(filter.UntilDate, result.UntilDate);
            Assert.Equal(filter.WonMatch, result.WonMatch);
        }

        [Fact]
        public void Page_number_defaults_to_1()
        {
            var parser = new StatisticsFilterQueryStringParser();

            var result = parser.ParseQueryString(new StatisticsFilter(), string.Empty);

            Assert.Equal(1, result.Paging.PageNumber);
        }

        [Fact]
        public void Page_number_is_parsed()
        {
            var parser = new StatisticsFilterQueryStringParser();

            var result = parser.ParseQueryString(new StatisticsFilter(), "page=5");

            Assert.Equal(5, result.Paging.PageNumber);
        }

        [Theory]
        [InlineData("?to=2021-12-31")]
        [InlineData("?from=")]
        [InlineData("?from=2021-02-31")]
        [InlineData("?from=invalid")]
        public void Missing_empty_or_invalid_FromDate_is_null(string queryString)
        {
            var parser = new StatisticsFilterQueryStringParser();

            var filter = parser.ParseQueryString(new StatisticsFilter(), queryString);

            Assert.Null(filter.FromDate);
        }

        [Fact]
        public void FromDate_is_parsed()
        {
            var parser = new StatisticsFilterQueryStringParser();

            var filter = parser.ParseQueryString(new StatisticsFilter(), "?from=2020-07-10");

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
            var parser = new StatisticsFilterQueryStringParser();

            var filter = parser.ParseQueryString(new StatisticsFilter(), queryString);

            Assert.Null(filter.UntilDate);
        }

        [Fact]
        public void UntilDate_is_parsed()
        {
            var parser = new StatisticsFilterQueryStringParser();

            var filter = parser.ParseQueryString(new StatisticsFilter(), "to=2022-06-09");

            Assert.NotNull(filter.UntilDate);
            Assert.Equal(new DateTime(2022, 6, 9), filter.UntilDate!.Value.Date);
        }

        [Theory]
        [InlineData("?")]
        [InlineData("?team=")]
        [InlineData("?team=invalid")]
        public void Missing_empty_or_invalid_team_is_null(string queryString)
        {
            var parser = new StatisticsFilterQueryStringParser();

            var filter = parser.ParseQueryString(new StatisticsFilter(), queryString);

            Assert.Null(filter.Team);
        }

        [Fact]
        public void Team_is_parsed()
        {
            var parser = new StatisticsFilterQueryStringParser();
            var teamId = Guid.NewGuid().ToString();

            var filter = parser.ParseQueryString(new StatisticsFilter(), $"?team={teamId}");

            Assert.Equal(teamId, filter.Team?.TeamId?.ToString());
        }
    }
}
