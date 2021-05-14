using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Comments;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Matches
{
    public class DeleteMatchSurfaceController : SurfaceController
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly IMatchRepository _matchRepository;
        private readonly ICommentsDataSource<Match> _matchCommentsDataSource;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;

        public DeleteMatchSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IMatchDataSource matchDataSource, IMatchRepository matchRepository,
           ICommentsDataSource<Match> matchCommentsDataSource, IAuthorizationPolicy<Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new System.ArgumentNullException(nameof(matchDataSource));
            _matchRepository = matchRepository ?? throw new System.ArgumentNullException(nameof(matchRepository));
            _matchCommentsDataSource = matchCommentsDataSource ?? throw new ArgumentNullException(nameof(matchCommentsDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> DeleteMatch([Bind(Prefix = "ConfirmDeleteRequest", Include = "RequiredText,ConfirmationText")] MatchingTextConfirmation postedModel)
        {
            if (postedModel is null)
            {
                throw new ArgumentNullException(nameof(postedModel));
            }

            var model = new DeleteMatchViewModel(CurrentPage, Services.UserService)
            {
                Match = await _matchDataSource.ReadMatchByRoute(Request.RawUrl).ConfigureAwait(false),
                DateTimeFormatter = _dateTimeFormatter
            };
            model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Match);

            if (model.IsAuthorized[AuthorizedAction.DeleteMatch] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _matchRepository.DeleteMatch(model.Match, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                model.Deleted = true;
            }
            else
            {
                model.TotalComments = await _matchCommentsDataSource.ReadTotalComments(model.Match.MatchId.Value).ConfigureAwait(false);
            }

            model.Metadata.PageTitle = "Delete " + model.Match.MatchFullName(x => _dateTimeFormatter.FormatDate(x.LocalDateTime, false, false, false)) + " - stoolball match";

            if (model.Match.Season != null)
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Season.Competition.CompetitionName, Url = new Uri(model.Match.Season.Competition.CompetitionRoute, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Season.SeasonName(), Url = new Uri(model.Match.Season.SeasonRoute, UriKind.Relative) });
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