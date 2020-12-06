using System;
using System.Threading.Tasks;
using System.Web.Mvc;
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
    public class DeleteTournamentSurfaceController : SurfaceController
    {
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly IMatchListingDataSource _matchListingDataSource;
        private readonly ITournamentRepository _tournamentRepository;
        private readonly ICommentsDataSource<Tournament> _commentsDataSource;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;

        public DeleteTournamentSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITournamentDataSource tournamentDataSource,
            IMatchListingDataSource matchListingDataSource, ITournamentRepository tournamentRepository,
           ICommentsDataSource<Tournament> matchCommentsDataSource, IAuthorizationPolicy<Tournament> authorizationPolicy, IDateTimeFormatter dateTimeFormatter)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _tournamentDataSource = tournamentDataSource ?? throw new System.ArgumentNullException(nameof(tournamentDataSource));
            _matchListingDataSource = matchListingDataSource ?? throw new ArgumentNullException(nameof(matchListingDataSource));
            _tournamentRepository = tournamentRepository ?? throw new System.ArgumentNullException(nameof(tournamentRepository));
            _commentsDataSource = matchCommentsDataSource ?? throw new ArgumentNullException(nameof(matchCommentsDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> DeleteTournament([Bind(Prefix = "ConfirmDeleteRequest", Include = "RequiredText,ConfirmationText")] MatchingTextConfirmation model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var viewModel = new DeleteTournamentViewModel(CurrentPage, Services.UserService)
            {
                Tournament = await _tournamentDataSource.ReadTournamentByRoute(Request.RawUrl).ConfigureAwait(false),
                DateTimeFormatter = _dateTimeFormatter
            };
            viewModel.IsAuthorized = _authorizationPolicy.IsAuthorized(viewModel.Tournament);

            if (viewModel.IsAuthorized[AuthorizedAction.DeleteTournament] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                await _tournamentRepository.DeleteTournament(viewModel.Tournament, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                viewModel.Deleted = true;
            }
            else
            {
                viewModel.TotalComments = await _commentsDataSource.ReadTotalComments(viewModel.Tournament.TournamentId.Value).ConfigureAwait(false);

                viewModel.Matches = new MatchListingViewModel
                {
                    Matches = await _matchListingDataSource.ReadMatchListings(new MatchQuery
                    {
                        TournamentId = viewModel.Tournament.TournamentId,
                        IncludeTournamentMatches = true,
                        IncludeTournaments = false
                    }).ConfigureAwait(false)
                };
            }

            viewModel.Metadata.PageTitle = "Delete " + viewModel.Tournament.TournamentFullNameAndPlayerType(x => _dateTimeFormatter.FormatDate(x.LocalDateTime, false, false, false));

            if (!viewModel.Deleted)
            {
                viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Tournament.TournamentName, Url = new Uri(viewModel.Tournament.TournamentRoute, UriKind.Relative) });
            }

            return View("DeleteTournament", viewModel);
        }
    }
}