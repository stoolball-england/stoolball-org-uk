using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Competitions
{
    public class MatchesForSeasonController : RenderMvcControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;

        public MatchesForSeasonController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ISeasonDataSource seasonDataSource,
           IMatchListingDataSource matchDataSource,
           IAuthorizationPolicy<Competition> authorizationPolicy,
           IDateTimeFormatter dateFormatter)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var season = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl, false).ConfigureAwait(false);

            if (season == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                var model = new SeasonViewModel(contentModel.Content, Services?.UserService)
                {
                    Season = season,
                    Matches = new MatchListingViewModel
                    {
                        Matches = await _matchDataSource.ReadMatchListings(new MatchQuery
                        {
                            SeasonIds = new List<Guid> { season.SeasonId.Value }
                        }).ConfigureAwait(false),
                        DateTimeFormatter = _dateFormatter
                    },
                };
                if (model.Season.MatchTypes.Contains(MatchType.LeagueMatch) || model.Season.MatchTypes.Contains(MatchType.KnockoutMatch))
                {
                    model.Matches.MatchTypesToLabel.Add(MatchType.FriendlyMatch);
                }

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Season.Competition);

                model.Metadata.PageTitle = $"Matches for {model.Season.SeasonFullNameAndPlayerType()}";

                return CurrentTemplate(model);
            }
        }
    }
}