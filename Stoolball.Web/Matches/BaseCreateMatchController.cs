using Humanizer;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Routing;
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

namespace Stoolball.Web.Matches
{
    public abstract class BaseCreateMatchController : RenderMvcControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ICreateMatchSeasonSelector _createMatchSeasonSelector;

        public BaseCreateMatchController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           ISeasonDataSource seasonDataSource,
           ICreateMatchSeasonSelector createMatchSeasonSelector)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _createMatchSeasonSelector = createMatchSeasonSelector ?? throw new ArgumentNullException(nameof(createMatchSeasonSelector));
        }

        protected async Task ConfigureModelForContextTeam(ICreateMatchViewModel model, MatchType matchType, bool populatePossibleTeams)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            model.Team = await _teamDataSource.ReadTeamByRoute(Request.Url.AbsolutePath, true).ConfigureAwait(false);
            model.HomeTeamId = model.Team?.TeamId;

            var initialLocation = model.Team?.MatchLocations.FirstOrDefault();
            if (initialLocation != null)
            {
                model.MatchLocationId = initialLocation.MatchLocationId;
                model.MatchLocationName = initialLocation.NameAndLocalityOrTownIfDifferent();
            }

            if (model.Team != null)
            {
                var possibleSeasons = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team.Seasons, matchType).ToList();
                if (possibleSeasons.Count > 0)
                {
                    model.PossibleSeasons.AddRange(possibleSeasons.Select(x => new SelectListItem { Text = x.SeasonFullName(), Value = x.SeasonId.Value.ToString() }));
                }

                if (populatePossibleTeams)
                {
                    var possibleTeams = new List<Team>();
                    foreach (var season in possibleSeasons)
                    {
                        var teamsInSeason = (await _seasonDataSource.ReadSeasonByRoute(season.SeasonRoute, true).ConfigureAwait(false))?.Teams.Where(x => x.WithdrawnDate == null).Select(x => x.Team);
                        if (teamsInSeason != null)
                        {
                            possibleTeams.AddRange(teamsInSeason);
                        }
                    }
                    model.PossibleTeams.AddRange(possibleTeams.OfType<Team>().Distinct(new TeamEqualityComparer()).Select(x => new SelectListItem { Text = x.TeamName, Value = x.TeamId.Value.ToString() }));

                    model.PossibleTeams.Sort(new TeamComparer(model.Team.TeamId));
                    if (model.PossibleTeams.Count > 1)
                    {
                        model.AwayTeamId = new Guid(model.PossibleTeams[1].Value);
                    }
                }
            }
        }

        protected async Task<ActionResult> ConfigureModelForContextSeason(ICreateMatchViewModel model, MatchType matchType)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            model.Season = await _seasonDataSource.ReadSeasonByRoute(Request.Url.AbsolutePath, true).ConfigureAwait(false);
            model.Match.Season = model.Season;
            if (model.Season == null || !model.Season.MatchTypes.Contains(matchType))
            {
                return new HttpNotFoundResult();
            }
            model.PossibleTeams.AddRange(model.Season.Teams.Select(x => new SelectListItem { Text = x.Team.TeamName, Value = x.Team.TeamId.Value.ToString() }));
            model.PossibleTeams.Sort(new TeamComparer(null));

            return null;
        }


        protected static void ConfigureModelMetadata(ICreateMatchViewModel model, MatchType matchType)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (model.Team != null)
            {
                model.Metadata.PageTitle = $"Add a {matchType.Humanize(LetterCasing.LowerCase)} for {model.Team.TeamName}";
            }
            else if (model.Season != null)
            {
                model.Metadata.PageTitle = $"Add a {matchType.Humanize(LetterCasing.LowerCase)} in the {model.Season.SeasonFullName()}";
            }
        }
    }
}