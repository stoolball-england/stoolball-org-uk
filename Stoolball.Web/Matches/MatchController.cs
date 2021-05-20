using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Comments;
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
    public class MatchController : RenderMvcControllerAsync
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly ICommentsDataSource<Match> _commentsDataSource;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IEmailProtector _emailProtector;

        public MatchController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           IMatchDataSource matchDataSource,
           ICommentsDataSource<Match> commentsDataSource,
           IAuthorizationPolicy<Match> authorizationPolicy,
           IDateTimeFormatter dateFormatter,
           IEmailProtector emailProtector)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _commentsDataSource = commentsDataSource ?? throw new ArgumentNullException(nameof(commentsDataSource));
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

            var model = new MatchViewModel(contentModel.Content, Services?.UserService)
            {
                Match = await _matchDataSource.ReadMatchByRoute(Request.Url.AbsolutePath).ConfigureAwait(false),
                DateTimeFormatter = _dateFormatter
            };

            if (model.Match == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Match);

                model.Match.Comments = await _commentsDataSource.ReadComments(model.Match.MatchId.Value).ConfigureAwait(false);

                model.Metadata.PageTitle = model.Match.MatchFullName(x => _dateFormatter.FormatDate(x, false, false, false)) + " - stoolball match";
                model.Metadata.Description = model.Match.Description();

                model.Match.MatchNotes = _emailProtector.ProtectEmailAddresses(model.Match.MatchNotes, User.Identity.IsAuthenticated);

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
                else
                {
                    model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Matches, Url = new Uri(Constants.Pages.MatchesUrl, UriKind.Relative) });
                }

                return CurrentTemplate(model);
            }
        }
    }
}