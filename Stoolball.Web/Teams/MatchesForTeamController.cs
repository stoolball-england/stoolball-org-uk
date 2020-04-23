using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Routing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Teams
{
    public class MatchesForTeamController : RenderMvcControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IEmailProtector _emailProtector;
        private readonly IDateFormatter _dateFormatter;
        private readonly IEstimatedSeason _estimatedSeason;

        public MatchesForTeamController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           IMatchDataSource matchDataSource,
           IEmailProtector emailProtector,
           IDateFormatter dateFormatter,
           IEstimatedSeason estimatedSeason)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _emailProtector = emailProtector ?? throw new ArgumentNullException(nameof(emailProtector));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _estimatedSeason = estimatedSeason ?? throw new ArgumentNullException(nameof(estimatedSeason));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var team = await _teamDataSource.ReadTeamByRoute(Request.Url.AbsolutePath).ConfigureAwait(false);

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
                            TeamIds = new List<int> { team.TeamId },
                            FromDate = _estimatedSeason.StartDate
                        }).ConfigureAwait(false),
                        DateFormatter = _dateFormatter
                    },
                };

                model.Metadata.PageTitle = $"Matches for {model.Team.TeamName} stoolball team";

                return CurrentTemplate(model);
            }
        }
    }
}