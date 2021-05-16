using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Dates;
using Stoolball.Email;
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

namespace Stoolball.Web.Matches
{
    public class TournamentController : RenderMvcControllerAsync
    {
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IEmailProtector _emailProtector;

        public TournamentController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITournamentDataSource tournamentDataSource,
           IMatchListingDataSource matchDataSource,
           IAuthorizationPolicy<Tournament> authorizationPolicy,
           IDateTimeFormatter dateFormatter,
           IEmailProtector emailProtector)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _tournamentDataSource = tournamentDataSource ?? throw new ArgumentNullException(nameof(tournamentDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _emailProtector = emailProtector ?? throw new ArgumentNullException(nameof(emailProtector));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new TournamentViewModel(contentModel.Content, Services?.UserService)
            {
                Tournament = await _tournamentDataSource.ReadTournamentByRoute(Request.Url.AbsolutePath).ConfigureAwait(false),
                DateTimeFormatter = _dateFormatter
            };

            if (model.Tournament == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Tournament);

                model.Matches = new MatchListingViewModel(contentModel.Content, Services?.UserService)
                {
                    Matches = await _matchDataSource.ReadMatchListings(new MatchFilter
                    {
                        TournamentId = model.Tournament.TournamentId,
                        IncludeTournamentMatches = true,
                        IncludeTournaments = false
                    }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false),
                    ShowMatchDate = false,
                    HighlightNextMatch = false,
                    DateTimeFormatter = _dateFormatter
                };

                model.Metadata.PageTitle = model.Tournament.TournamentFullNameAndPlayerType(x => _dateFormatter.FormatDate(x, false, false, false));
                model.Metadata.Description = model.Tournament.Description();

                model.Tournament.TournamentNotes = _emailProtector.ProtectEmailAddresses(model.Tournament.TournamentNotes, User.Identity.IsAuthenticated);

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}