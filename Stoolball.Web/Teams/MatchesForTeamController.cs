using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Matches;
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
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Web.Teams
{
    public class MatchesForTeamController : RenderMvcControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IEstimatedSeason _estimatedSeason;
        private readonly ICreateLeagueMatchEligibleSeasons _createLeagueMatchEligibleSeasons;

        public MatchesForTeamController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           IMatchListingDataSource matchDataSource,
           IDateTimeFormatter dateFormatter,
           IEstimatedSeason estimatedSeason,
           ICreateLeagueMatchEligibleSeasons createLeagueMatchEligibleSeasons)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _estimatedSeason = estimatedSeason ?? throw new ArgumentNullException(nameof(estimatedSeason));
            _createLeagueMatchEligibleSeasons = createLeagueMatchEligibleSeasons ?? throw new ArgumentNullException(nameof(createLeagueMatchEligibleSeasons));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var team = await _teamDataSource.ReadTeamByRoute(Request.Url.AbsolutePath, true).ConfigureAwait(false);

            if (team == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                var model = new TeamViewModel(contentModel.Content)
                {
                    Team = team,
                    Matches = new MatchListingViewModel
                    {
                        Matches = await _matchDataSource.ReadMatchListings(new MatchQuery
                        {
                            TeamIds = new List<Guid> { team.TeamId.Value },
                            FromDate = _estimatedSeason.StartDate
                        }).ConfigureAwait(false),
                        DateTimeFormatter = _dateFormatter
                    },
                };

                model.IsAuthorized = IsAuthorized(model);
                model.IsInACurrentLeague = _createLeagueMatchEligibleSeasons.SelectEligibleSeasons(model.Team.Seasons).Any();

                model.Metadata.PageTitle = $"Matches for {model.Team.TeamName} stoolball team";

                return CurrentTemplate(model);
            }
        }

        /// <summary>
        /// Checks whether the currently signed-in member is authorized to edit this team
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized(TeamViewModel model)
        {
            return Members.IsMemberAuthorized(null, new[] { Groups.Administrators, model?.Team.MemberGroupName }, null);
        }
    }
}