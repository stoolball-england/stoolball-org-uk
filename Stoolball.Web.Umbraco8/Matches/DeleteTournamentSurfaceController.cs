using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Caching;
using Stoolball.Comments;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Matches
{
    public class DeleteTournamentSurfaceController : SurfaceController
    {
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly IMatchListingDataSource _matchListingDataSource;
        private readonly ITournamentRepository _tournamentRepository;
        private readonly ICacheClearer<Tournament> _cacheClearer;
        private readonly ICommentsDataSource<Tournament> _commentsDataSource;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;

        public DeleteTournamentSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITournamentDataSource tournamentDataSource,
            IMatchListingDataSource matchListingDataSource, ITournamentRepository tournamentRepository, ICacheClearer<Tournament> cacheClearer,
           ICommentsDataSource<Tournament> matchCommentsDataSource, IAuthorizationPolicy<Tournament> authorizationPolicy, IDateTimeFormatter dateTimeFormatter)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _tournamentDataSource = tournamentDataSource ?? throw new ArgumentNullException(nameof(tournamentDataSource));
            _matchListingDataSource = matchListingDataSource ?? throw new ArgumentNullException(nameof(matchListingDataSource));
            _tournamentRepository = tournamentRepository ?? throw new ArgumentNullException(nameof(tournamentRepository));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
            _commentsDataSource = matchCommentsDataSource ?? throw new ArgumentNullException(nameof(matchCommentsDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> DeleteTournament([Bind(Prefix = "ConfirmDeleteRequest", Include = "RequiredText,ConfirmationText")] Stoolball.Security.MatchingTextConfirmation postedModel)
        {
            if (postedModel is null)
            {
                throw new ArgumentNullException(nameof(postedModel));
            }

            var model = new DeleteTournamentViewModel(CurrentPage, Services.UserService)
            {
                Tournament = await _tournamentDataSource.ReadTournamentByRoute(Request.RawUrl).ConfigureAwait(false),
                DateTimeFormatter = _dateTimeFormatter
            };
            model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Tournament);

            if (model.IsAuthorized[Stoolball.Security.AuthorizedAction.DeleteTournament] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _tournamentRepository.DeleteTournament(model.Tournament, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                await _cacheClearer.ClearCacheFor(model.Tournament).ConfigureAwait(false);
                model.Deleted = true;
            }
            else
            {
                model.TotalComments = await _commentsDataSource.ReadTotalComments(model.Tournament.TournamentId.Value).ConfigureAwait(false);

                model.Matches = new MatchListingViewModel(CurrentPage, Services?.UserService)
                {
                    Matches = await _matchListingDataSource.ReadMatchListings(new MatchFilter
                    {
                        TournamentId = model.Tournament.TournamentId,
                        IncludeTournamentMatches = true,
                        IncludeTournaments = false
                    }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false)
                };
            }

            model.Metadata.PageTitle = "Delete " + model.Tournament.TournamentFullNameAndPlayerType(x => _dateTimeFormatter.FormatDate(x, false, false, false));

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });
            if (!model.Deleted)
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Tournament.TournamentName, Url = new Uri(model.Tournament.TournamentRoute, UriKind.Relative) });
            }

            return View("DeleteTournament", model);
        }
    }
}