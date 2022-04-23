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
    public class MatchController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly ICommentsDataSource<Match> _commentsDataSource;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IApiKeyProvider _apiKeyProvider;
        private readonly IEmailProtector _emailProtector;
        private readonly IBadLanguageFilter _badLanguageFilter;

        public MatchController(ILogger<MatchController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchDataSource matchDataSource,
            ICommentsDataSource<Match> commentsDataSource,
            IAuthorizationPolicy<Match> authorizationPolicy,
            IDateTimeFormatter dateFormatter,
            IApiKeyProvider apiKeyProvider,
            IEmailProtector emailProtector,
            IBadLanguageFilter badLanguageFilter)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
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
            var model = new MatchViewModel(CurrentPage)
            {
                Match = await _matchDataSource.ReadMatchByRoute(Request.Path),
                GoogleMapsApiKey = _apiKeyProvider.GetApiKey("GoogleMaps")
            };

            if (model.Match == null)
            {
                return NotFound();
            }
            else
            {
                model.IsAuthorized = await _authorizationPolicy.IsAuthorized(model.Match);

                model.Match.Comments = await _commentsDataSource.ReadComments(model.Match.MatchId!.Value).ConfigureAwait(false);
                foreach (var comment in model.Match.Comments)
                {
                    comment.Comment = _emailProtector.ProtectEmailAddresses(comment.Comment, User.Identity?.IsAuthenticated ?? false);
                    comment.Comment = _badLanguageFilter.Filter(comment.Comment);
                }

                model.Metadata.PageTitle = model.Match.MatchFullName(x => _dateFormatter.FormatDate(x, false, false, false)) + " - stoolball match";
                model.Metadata.Description = model.Match.Description();

                model.Match.MatchNotes = _emailProtector.ProtectEmailAddresses(model.Match.MatchNotes, User.Identity?.IsAuthenticated ?? false);

                // If a team was all out, convert wickets to -1. This value is used by the view, and also by matches imported from the old website.
                foreach (var innings in model.Match.MatchInnings)
                {
                    if (innings.Wickets.HasValue)
                    {
                        if ((innings.Wickets == model.Match.PlayersPerTeam - 1 && !model.Match.LastPlayerBatsOn) ||
                            (innings.Wickets == model.Match.PlayersPerTeam && model.Match.LastPlayerBatsOn))
                        {
                            innings.Wickets = -1;
                        }
                    }
                }

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

                return CurrentTemplate(model);
            }
        }
    }
}