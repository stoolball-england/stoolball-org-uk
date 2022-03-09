using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Caching;
using Stoolball.Comments;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Website.Controllers;

namespace Stoolball.Web.Matches
{
    public class DeleteMatchSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IMatchRepository _matchRepository;
        private readonly ICommentsDataSource<Match> _matchCommentsDataSource;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly ICacheClearer<Match> _cacheClearer;

        public DeleteMatchSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IMatchDataSource matchDataSource, IMatchRepository matchRepository, ICommentsDataSource<Match> matchCommentsDataSource,
            IAuthorizationPolicy<Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter, ICacheClearer<Match> cacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _matchCommentsDataSource = matchCommentsDataSource ?? throw new ArgumentNullException(nameof(matchCommentsDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> DeleteMatch([Bind("RequiredText", "ConfirmationText", Prefix = "ConfirmDeleteRequest")] MatchingTextConfirmation postedModel)
        {
            if (postedModel is null)
            {
                throw new ArgumentNullException(nameof(postedModel));
            }

            var model = new DeleteMatchViewModel(CurrentPage, Services.UserService)
            {
                Match = await _matchDataSource.ReadMatchByRoute(Request.Path)
            };
            model.IsAuthorized = await _authorizationPolicy.IsAuthorized(model.Match);

            if (model.IsAuthorized[AuthorizedAction.DeleteMatch] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                await _matchRepository.DeleteMatch(model.Match, currentMember.Key, currentMember.Name);
                await _cacheClearer.ClearCacheFor(model.Match);
                model.Deleted = true;
            }
            else
            {
                model.TotalComments = await _matchCommentsDataSource.ReadTotalComments(model.Match.MatchId!.Value);
            }

            model.Metadata.PageTitle = "Delete " + model.Match.MatchFullName(x => _dateTimeFormatter.FormatDate(x, false, false, false)) + " - stoolball match";

            if (model.Match.Season != null)
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Season.Competition.CompetitionName, Url = new Uri(model.Match.Season.Competition.CompetitionRoute, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Season.SeasonName(), Url = new Uri(model.Match.Season.SeasonRoute, UriKind.Relative) });
            }
            else if (model.Match.Tournament != null)
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Tournament.TournamentName, Url = new Uri(model.Match.Tournament.TournamentRoute, UriKind.Relative) });
            }
            else
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Matches, Url = new Uri(Constants.Pages.MatchesUrl, UriKind.Relative) });
            }
            if (!model.Deleted)
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.MatchName, Url = new Uri(model.Match.MatchRoute, UriKind.Relative) });
            }

            return View("DeleteMatch", model);
        }
    }
}