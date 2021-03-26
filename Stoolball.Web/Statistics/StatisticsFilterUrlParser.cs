using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.MatchLocations;
using Stoolball.Navigation;
using Stoolball.Routing;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Web.Statistics
{
    public class StatisticsFilterUrlParser : IStatisticsFilterUrlParser
    {
        private readonly IPlayerDataSource _playerDataSource;
        private readonly IClubDataSource _clubDataSource;
        private readonly ITeamDataSource _teamDataSource;
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IRouteNormaliser _routeNormaliser;

        public StatisticsFilterUrlParser(IPlayerDataSource playerDataSource,
           IClubDataSource clubDataSource,
           ITeamDataSource teamDataSource,
           IMatchLocationDataSource matchLocationDataSource,
           ICompetitionDataSource competitionDataSource,
           ISeasonDataSource seasonDataSource,
           IRouteNormaliser routeNormaliser)
        {
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
            _clubDataSource = clubDataSource ?? throw new ArgumentNullException(nameof(clubDataSource));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        public async Task<StatisticsFilter> ParseUrl(Uri url)
        {
            if (url == null) { throw new ArgumentNullException(nameof(url)); }
            if (!url.IsAbsoluteUri) { throw new ArgumentException($"{nameof(url)} must be an absolute URL", nameof(url)); }

            var queryString = HttpUtility.ParseQueryString(url.Query);
            _ = int.TryParse(queryString["page"], out var pageNumber);

            var filter = new StatisticsFilter { Paging = new Paging { PageNumber = pageNumber > 0 ? pageNumber : 1 } };
            if (url.AbsolutePath.StartsWith("/players/", StringComparison.OrdinalIgnoreCase))
            {
                filter.Player = await _playerDataSource.ReadPlayerByRoute(_routeNormaliser.NormaliseRouteToEntity(url.AbsolutePath, "players")).ConfigureAwait(false);
            }
            else if (url.AbsolutePath.StartsWith("/clubs/", StringComparison.OrdinalIgnoreCase))
            {
                filter.Club = await _clubDataSource.ReadClubByRoute(_routeNormaliser.NormaliseRouteToEntity(url.AbsolutePath, "clubs")).ConfigureAwait(false);
            }
            else if (url.AbsolutePath.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                filter.Team = await _teamDataSource.ReadTeamByRoute(_routeNormaliser.NormaliseRouteToEntity(url.AbsolutePath, "teams"), false).ConfigureAwait(false);
            }
            else if (url.AbsolutePath.StartsWith("/locations/", StringComparison.OrdinalIgnoreCase))
            {
                filter.MatchLocation = await _matchLocationDataSource.ReadMatchLocationByRoute(_routeNormaliser.NormaliseRouteToEntity(url.AbsolutePath, "locations"), false).ConfigureAwait(false);
            }
            else if (Regex.IsMatch(url.AbsolutePath, @"\/competitions\/" + Constants.Pages.SeasonUrlRegEx.TrimStart('^').TrimEnd('$'), RegexOptions.IgnoreCase))
            {
                filter.Season = await _seasonDataSource.ReadSeasonByRoute(_routeNormaliser.NormaliseRouteToEntity(url.AbsolutePath, "competitions", Constants.Pages.SeasonUrlRegEx)).ConfigureAwait(false);
            }
            else if (url.AbsolutePath.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                filter.Competition = await _competitionDataSource.ReadCompetitionByRoute(_routeNormaliser.NormaliseRouteToEntity(url.AbsolutePath, "competitions")).ConfigureAwait(false);
            }
            return filter;
        }
    }
}