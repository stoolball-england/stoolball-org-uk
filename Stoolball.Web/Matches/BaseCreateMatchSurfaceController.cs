using Humanizer;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Teams;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Matches
{
    public abstract class BaseCreateMatchSurfaceController : SurfaceController
    {
        private readonly ICreateMatchSeasonSelector _createMatchSeasonSelector;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;

        public BaseCreateMatchSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITeamDataSource teamDataSource, ISeasonDataSource seasonDataSource,
            ICreateMatchSeasonSelector createMatchSeasonSelector)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _createMatchSeasonSelector = createMatchSeasonSelector ?? throw new ArgumentNullException(nameof(createMatchSeasonSelector));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
        }


        protected void ConfigureModelFromRequestData(ICreateMatchViewModel model, Match postedMatch)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (postedMatch is null)
            {
                throw new ArgumentNullException(nameof(postedMatch));
            }

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            postedMatch.MatchNotes = Request.Unvalidated.Form["Match.MatchNotes"];

            if (!string.IsNullOrEmpty(Request.Form["MatchDate"]))
            {
                model.MatchDate = DateTimeOffset.Parse(Request.Form["MatchDate"], CultureInfo.CurrentCulture);
                postedMatch.StartTime = model.MatchDate.Value;
                postedMatch.StartTimeIsKnown = false;
                if (!string.IsNullOrEmpty(Request.Form["StartTime"]))
                {
                    model.StartTime = DateTimeOffset.Parse(Request.Form["StartTime"], CultureInfo.CurrentCulture);
                    postedMatch.StartTime = postedMatch.StartTime.Add(model.StartTime.Value.TimeOfDay);
                    postedMatch.StartTimeIsKnown = true;
                }
            }
            if (!string.IsNullOrEmpty(Request.Form["HomeTeamId"]))
            {
                model.HomeTeamId = new Guid(Request.Form["HomeTeamId"]);
                postedMatch.Teams.Add(new TeamInMatch
                {
                    Team = new Team { TeamId = model.HomeTeamId },
                    TeamRole = TeamRole.Home
                });
            }
            if (!string.IsNullOrEmpty(Request.Form["AwayTeamId"]))
            {
                model.AwayTeamId = new Guid(Request.Form["AwayTeamId"]);
                postedMatch.Teams.Add(new TeamInMatch
                {
                    Team = new Team { TeamId = model.AwayTeamId },
                    TeamRole = TeamRole.Away
                });
            }
            if (!string.IsNullOrEmpty(Request.Form["MatchLocationId"]))
            {
                model.MatchLocationId = new Guid(Request.Form["MatchLocationId"]);
                model.MatchLocationName = Request.Form["MatchLocationName"];
                postedMatch.MatchLocation = new MatchLocation
                {
                    MatchLocationId = model.MatchLocationId
                };
            }
        }

        protected async Task ConfigureModelForRedisplay(ICreateMatchViewModel model, MatchType matchType)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (Request.RawUrl.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                model.Team = await _teamDataSource.ReadTeamByRoute(Request.RawUrl, true).ConfigureAwait(false);
                var possibleSeasons = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team?.Seasons, matchType).ToList();
                if (possibleSeasons.Count == 1)
                {
                    model.Match.Season = possibleSeasons[0];
                }
                model.PossibleSeasons.AddRange(possibleSeasons.Select(x => new SelectListItem { Text = x.SeasonFullName(), Value = x.SeasonId.Value.ToString() }));

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

                model.Metadata.PageTitle = $"Add a {matchType.Humanize(LetterCasing.LowerCase)} for {model.Team.TeamName}";
            }
            else if (Request.RawUrl.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                model.Season = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl, true).ConfigureAwait(false);
                model.PossibleTeams.AddRange(model.Season.Teams.Select(x => new SelectListItem { Text = x.Team.TeamName, Value = x.Team.TeamId.Value.ToString() }));
                model.PossibleTeams.Sort(new TeamComparer(null));
                model.Metadata.PageTitle = $"Add a {matchType.Humanize(LetterCasing.LowerCase)} in the {model.Season.SeasonFullName()}";
            }
        }
    }
}