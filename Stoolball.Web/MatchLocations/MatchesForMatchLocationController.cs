using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.MatchLocations;
using Stoolball.Web.Matches;
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
using Umbraco.Web.Models;

namespace Stoolball.Web.MatchLocations
{
    public class MatchesForMatchLocationController : RenderMvcControllerAsync
    {
        private readonly IMatchLocationDataSource _matchLocationDataSource;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IDateFormatter _dateFormatter;
        private readonly IEstimatedSeason _estimatedSeason;

        public MatchesForMatchLocationController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchLocationDataSource matchLocationDataSource,
           IMatchDataSource matchDataSource,
           IDateFormatter dateFormatter,
           IEstimatedSeason estimatedSeason)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchLocationDataSource = matchLocationDataSource ?? throw new ArgumentNullException(nameof(matchLocationDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _estimatedSeason = estimatedSeason ?? throw new ArgumentNullException(nameof(estimatedSeason));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var location = await _matchLocationDataSource.ReadMatchLocationByRoute(Request.Url.AbsolutePath, false).ConfigureAwait(false);

            if (location == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                var model = new MatchLocationViewModel(contentModel.Content)
                {
                    MatchLocation = location,
                    Matches = new MatchListingViewModel
                    {
                        Matches = await _matchDataSource.ReadMatchListings(new MatchQuery
                        {
                            TeamIds = location.Teams.Select(team => team.TeamId.Value).ToList(),
                            FromDate = _estimatedSeason.StartDate,
                            ExcludeMatchTypes = new List<MatchType> { MatchType.TournamentMatch }
                        }).ConfigureAwait(false),
                        DateFormatter = _dateFormatter
                    },
                };

                model.Metadata.PageTitle = $"Matches for {model.MatchLocation}";

                return CurrentTemplate(model);
            }
        }
    }
}