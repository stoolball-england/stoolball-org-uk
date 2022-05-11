using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Comments;
using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Html;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Configuration;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Matches
{
    public class TournamentController : RenderController, IRenderControllerAsync
    {
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IMatchFilterFactory _matchFilterFactory;
        private readonly ICommentsDataSource<Tournament> _commentsDataSource;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IApiKeyProvider _apiKeyProvider;
        private readonly IEmailProtector _emailProtector;
        private readonly IBadLanguageFilter _badLanguageFilter;

        public TournamentController(ILogger<TournamentController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITournamentDataSource tournamentDataSource,
            IMatchListingDataSource matchDataSource,
            IMatchFilterFactory matchFilterFactory,
            ICommentsDataSource<Tournament> commentsDataSource,
            IAuthorizationPolicy<Tournament> authorizationPolicy,
            IDateTimeFormatter dateFormatter,
            IApiKeyProvider apiKeyProvider,
            IEmailProtector emailProtector,
            IBadLanguageFilter badLanguageFilter)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _tournamentDataSource = tournamentDataSource ?? throw new ArgumentNullException(nameof(tournamentDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchFilterFactory = matchFilterFactory ?? throw new ArgumentNullException(nameof(matchFilterFactory));
            _commentsDataSource = commentsDataSource ?? throw new ArgumentNullException(nameof(commentsDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
            _apiKeyProvider = apiKeyProvider ?? throw new ArgumentNullException(nameof(apiKeyProvider));
            _emailProtector = emailProtector ?? throw new ArgumentNullException(nameof(emailProtector));
            _badLanguageFilter = badLanguageFilter ?? throw new ArgumentNullException(nameof(badLanguageFilter));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new TournamentViewModel(CurrentPage)
            {
                Tournament = await _tournamentDataSource.ReadTournamentByRoute(Request.Path),
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };

            if (model.Tournament == null)
            {
                return NotFound();
            }
            else
            {
                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Tournament);

                model.Tournament.Comments = await _commentsDataSource.ReadComments(model.Tournament.TournamentId!.Value);
                foreach (var comment in model.Tournament.Comments)
                {
                    comment.Comment = _emailProtector.ProtectEmailAddresses(comment.Comment, User.Identity?.IsAuthenticated ?? false);
                    comment.Comment = _badLanguageFilter.Filter(comment.Comment);
                }

                var filter = _matchFilterFactory.MatchesForTournament(model.Tournament.TournamentId.Value);
                model.Matches = new MatchListingViewModel(CurrentPage)
                {
                    Matches = await _matchDataSource.ReadMatchListings(filter.filter, filter.sortOrder),
                    ShowMatchDate = false,
                    HighlightNextMatch = false
                };

                model.Metadata.PageTitle = model.Tournament.TournamentFullNameAndPlayerType(x => _dateFormatter.FormatDate(x, false, false, false));
                model.Metadata.Description = model.Tournament.Description();

                model.Tournament.TournamentNotes = _emailProtector.ProtectEmailAddresses(model.Tournament.TournamentNotes, User.Identity?.IsAuthenticated ?? false);

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}