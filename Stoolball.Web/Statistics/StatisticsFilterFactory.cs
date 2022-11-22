using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Stoolball.Data.Abstractions;
using Stoolball.Navigation;
using Stoolball.Routing;
using Stoolball.Statistics;

namespace Stoolball.Web.Statistics
{
    public class StatisticsFilterFactory : IStatisticsFilterFactory
    {
        private readonly IStoolballEntityRouteParser _stoolballEntityRouteParser;
        private readonly IStatisticsFilterQueryStringParser _queryStringParser;
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IClubDataSource _clubDataSource;
        private readonly ITeamDataSource _teamDataSource;
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IRouteNormaliser _routeNormaliser;

        public StatisticsFilterFactory(
           IStoolballEntityRouteParser stoolballEntityRouteParser,
           IStatisticsFilterQueryStringParser queryStringParser,
           IPlayerDataSource playerDataSource,
           IClubDataSource clubDataSource,
           ITeamDataSource teamDataSource,
           IMatchLocationDataSource matchLocationDataSource,
           ICompetitionDataSource competitionDataSource,
           ISeasonDataSource seasonDataSource,
           IRouteNormaliser routeNormaliser)
        {
            _stoolballEntityRouteParser = stoolballEntityRouteParser ?? throw new ArgumentNullException(nameof(stoolballEntityRouteParser));
            _queryStringParser = queryStringParser ?? throw new ArgumentNullException(nameof(queryStringParser));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        public async Task<StatisticsFilter> FromRoute(string route)
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                throw new ArgumentException($"'{nameof(route)}' cannot be null or whitespace.", nameof(route));
            }

            var entityType = _stoolballEntityRouteParser.ParseRoute(route);

            var filter = new StatisticsFilter { Paging = new Paging { PageSize = Constants.Defaults.PageSize } };
            if (route.Contains("player-of-match", StringComparison.OrdinalIgnoreCase))
            {
                filter.PlayerOfTheMatch = true;
            }
            if (route.Contains("most-scores-of-", StringComparison.OrdinalIgnoreCase))
            {
                var mostScoresOf = Regex.Match(route, "most-scores-of-(?<MinimumRunsScored>[0-9]{1,3})", RegexOptions.IgnoreCase);
                filter.MinimumRunsScored = mostScoresOf.Success ? int.Parse(mostScoresOf.Groups["MinimumRunsScored"].Value) : null;
            }
            var mostXWickets = Regex.Match(route, "most-(?<MinimumWickets>[0-9]{1,2})-wickets", RegexOptions.IgnoreCase);
            if (mostXWickets.Success)
            {
                filter.MinimumWicketsTaken = int.Parse(mostXWickets.Groups["MinimumWickets"].Value);
            }
            if (entityType == StoolballEntityType.Player)
            {
                filter.Player = await _playerDataSource.ReadPlayerByRoute(_routeNormaliser.NormaliseRouteToEntity(route, "players")).ConfigureAwait(false);
            }
            else if (entityType == StoolballEntityType.Club)
            {
                filter.Club = await _clubDataSource.ReadClubByRoute(_routeNormaliser.NormaliseRouteToEntity(route, "clubs")).ConfigureAwait(false);
            }
            else if (entityType == StoolballEntityType.Team)
            {
                filter.Team = await _teamDataSource.ReadTeamByRoute(_routeNormaliser.NormaliseRouteToEntity(route, "teams"), true).ConfigureAwait(false); // true gets a lot of data but we only really want the club
            }
            else if (entityType == StoolballEntityType.MatchLocation)
            {
                filter.MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(_routeNormaliser.NormaliseRouteToEntity(route, "locations"), false).ConfigureAwait(false);
            }
            else if (entityType == StoolballEntityType.Season)
            {
                filter.Season = await _seasonDataSource.ReadSeasonByRoute(_routeNormaliser.NormaliseRouteToEntity(route, "competitions", Constants.Pages.SeasonUrlRegEx), true).ConfigureAwait(false); // true gets the teams
            }
            else if (entityType == StoolballEntityType.Competition)
            {
                filter.Competition = await _competitionDataSource.ReadCompetitionByRoute(_routeNormaliser.NormaliseRouteToEntity(route, "competitions")).ConfigureAwait(false);
            }
            return filter;
        }

        public async Task<StatisticsFilter> FromQueryString(string? queryString)
        {
            var filter = _queryStringParser.ParseQueryString(queryString);

            if (!string.IsNullOrEmpty(filter.Team?.TeamRoute))
            {
                filter.Team = await _teamDataSource.ReadTeamByRoute(_routeNormaliser.NormaliseRouteToEntity(filter.Team.TeamRoute, "teams"), true).ConfigureAwait(false);
            }

            return filter;
        }

    }
}