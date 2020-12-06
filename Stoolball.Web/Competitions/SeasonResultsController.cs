using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Matches;
using Stoolball.Navigation;
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
    public class SeasonResultsController : RenderMvcControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;
        private readonly IEmailProtector _emailProtector;
        private readonly IDateTimeFormatter _dateTimeFormatter;

        public SeasonResultsController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ISeasonDataSource seasonDataSource,
           IMatchListingDataSource matchDataSource,
           IAuthorizationPolicy<Competition> authorizationPolicy,
           IEmailProtector emailProtector,
           IDateTimeFormatter dateTimeFormatter
           )
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _emailProtector = emailProtector ?? throw new ArgumentNullException(nameof(emailProtector));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new SeasonViewModel(contentModel.Content, Services?.UserService)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl, true).ConfigureAwait(false)
            };

            if (model.Season == null || (!model.Season.MatchTypes.Contains(MatchType.LeagueMatch) &&
                !model.Season.MatchTypes.Contains(MatchType.KnockoutMatch) &&
                !model.Season.MatchTypes.Contains(MatchType.FriendlyMatch) &&
                string.IsNullOrEmpty(model.Season.Results)))
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.Matches = new MatchListingViewModel
                {
                    Matches = await _matchDataSource.ReadMatchListings(new MatchQuery
                    {
                        SeasonIds = new List<Guid> { model.Season.SeasonId.Value },
                        IncludeTournaments = false
                    }).ConfigureAwait(false),
                    DateTimeFormatter = _dateTimeFormatter
                };
                model.Season.PointsRules.AddRange(await _seasonDataSource.ReadPointsRules(model.Season.SeasonId.Value).ConfigureAwait(false));
                model.Season.PointsAdjustments.AddRange(await _seasonDataSource.ReadPointsAdjustments(model.Season.SeasonId.Value).ConfigureAwait(false));

                model.Season.Results = _emailProtector.ProtectEmailAddresses(model.Season.Results, User.Identity.IsAuthenticated);

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Season.Competition);

                var the = model.Season.Competition.CompetitionName.StartsWith("THE ", StringComparison.OrdinalIgnoreCase);
                model.Metadata.PageTitle = $"Results for {(the ? string.Empty : "the ")}{model.Season.SeasonFullNameAndPlayerType()}";
                model.Metadata.Description = model.Season.Description();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.Competition.CompetitionName, Url = new Uri(model.Season.Competition.CompetitionRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}