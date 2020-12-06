using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
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
    public class DeleteSeasonController : RenderMvcControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public DeleteSeasonController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ISeasonDataSource seasonDataSource,
           IMatchListingDataSource matchDataSource,
           IAuthorizationPolicy<Competition> authorizationPolicy)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new DeleteSeasonViewModel(contentModel.Content, Services?.UserService)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.RawUrl, true).ConfigureAwait(false)
            };

            if (model.Season == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.TotalMatches = await _matchDataSource.ReadTotalMatches(new MatchQuery
                {
                    SeasonIds = new List<Guid> { model.Season.SeasonId.Value },
                    IncludeTournamentMatches = true
                }).ConfigureAwait(false);

                model.ConfirmDeleteRequest.RequiredText = model.Season.SeasonFullName();

                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Season.Competition);

                model.Metadata.PageTitle = "Delete " + model.Season.SeasonFullNameAndPlayerType();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.Competition.CompetitionName, Url = new Uri(model.Season.Competition.CompetitionRoute, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.SeasonName(), Url = new Uri(model.Season.SeasonRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}