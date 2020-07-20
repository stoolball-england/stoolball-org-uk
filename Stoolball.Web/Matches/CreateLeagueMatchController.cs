using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Matches
{
    public class CreateLeagueMatchController : RenderMvcControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ICreateLeagueMatchEligibleSeasons _createLeagueMatchEligibleSeasons;

        public CreateLeagueMatchController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           ISeasonDataSource seasonDataSource,
           ICreateLeagueMatchEligibleSeasons createLeagueMatchEligibleSeasons)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _createLeagueMatchEligibleSeasons = createLeagueMatchEligibleSeasons ?? throw new ArgumentNullException(nameof(createLeagueMatchEligibleSeasons));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new CreateMatchViewModel(contentModel.Content) { Match = new Match { MatchLocation = new MatchLocation() } };
            if (Request.Url.AbsolutePath.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                model.Team = await _teamDataSource.ReadTeamByRoute(Request.Url.AbsolutePath, true).ConfigureAwait(false);
                if (model.Team == null)
                {
                    return new HttpNotFoundResult();
                }

                var possibleSeasons = _createLeagueMatchEligibleSeasons.SelectEligibleSeasons(model.Team.Seasons).ToList();
                if (possibleSeasons == null || !possibleSeasons.Any())
                {
                    return new HttpNotFoundResult();
                }
                if (possibleSeasons.Count == 1)
                {
                    model.Match.Season = possibleSeasons[0];
                }
                model.PossibleSeasons = possibleSeasons
                    .Select(x => new SelectListItem { Text = x.SeasonFullName(), Value = x.SeasonId.Value.ToString() })
                    .ToList();

                var possibleTeams = new List<Team>();
                foreach (var season in possibleSeasons)
                {
                    possibleTeams.AddRange((await _seasonDataSource.ReadSeasonByRoute(season.SeasonRoute, true).ConfigureAwait(false))?.Teams.Where(x => x.WithdrawnDate == null).Select(x => x.Team));
                }
                model.PossibleTeams = possibleTeams.OfType<Team>().Distinct(new TeamEqualityComparer()).Select(x => new SelectListItem { Text = x.TeamName, Value = x.TeamId.Value.ToString() }).ToList();

                model.PossibleTeams.Sort(new TeamComparer(model.Team.TeamId));
                model.HomeTeamId = model.Team.TeamId;
                if (model.PossibleTeams.Count > 1)
                {
                    model.AwayTeamId = new Guid(model.PossibleTeams[1].Value);
                }
            }
            else if (Request.Url.AbsolutePath.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                model.Season = await _seasonDataSource.ReadSeasonByRoute(Request.Url.AbsolutePath, true).ConfigureAwait(false);
                model.Match.Season = model.Season;
                if (model.Season == null || !model.Season.MatchTypes.Contains(MatchType.LeagueMatch))
                {
                    return new HttpNotFoundResult();
                }
                model.PossibleTeams = model.Season.Teams.Select(x => new SelectListItem { Text = x.Team.TeamName, Value = x.Team.TeamId.Value.ToString() }).ToList();
                model.PossibleTeams.Sort(new TeamComparer(null));
                if (model.PossibleTeams.Count > 0)
                {
                    model.HomeTeamId = new Guid(model.PossibleTeams[0].Value);
                }
                if (model.PossibleTeams.Count > 1)
                {
                    model.AwayTeamId = new Guid(model.PossibleTeams[1].Value);
                }
            }


            model.IsAuthorized = User.Identity.IsAuthenticated;

            if (model.Team != null)
            {
                model.Metadata.PageTitle = $"Add a league match for {model.Team.TeamName}";
            }
            else if (model.Season != null)
            {
                model.Metadata.PageTitle = $"Add a league match in the {model.Season.SeasonFullName()}";
            }

            return CurrentTemplate(model);
        }
    }
}