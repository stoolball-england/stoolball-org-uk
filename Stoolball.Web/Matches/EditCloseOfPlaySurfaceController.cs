using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Awards;
using Stoolball.Caching;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Teams;
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
    public class EditCloseOfPlaySurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IMatchRepository _matchRepository;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly ICacheClearer<Match> _cacheClearer;

        public EditCloseOfPlaySurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IMatchDataSource matchDataSource, IMatchRepository matchRepository,
            IAuthorizationPolicy<Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter, ICacheClearer<Match> cacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateMatch([Bind(Prefix = "FormData")] EditCloseOfPlayFormData postedData)
        {
            if (postedData is null)
            {
                throw new ArgumentNullException(nameof(postedData));
            }

            var beforeUpdate = await _matchDataSource.ReadMatchByRoute(Request.Path);

            if (beforeUpdate.StartTime > DateTime.UtcNow || beforeUpdate.Tournament != null)
            {
                return NotFound();
            }

            var model = new EditCloseOfPlayViewModel(CurrentPage, Services.UserService)
            {
                Match = beforeUpdate,
                FormData = postedData
            };
            model.Match.MatchResultType = model.FormData.MatchResultType;
            model.Match.Awards = model.FormData.Awards.Select(x => new MatchAward
            {
                AwardedToId = x.MatchAwardId,
                Award = new Award { AwardName = StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD },
                PlayerIdentity = new PlayerIdentity
                {
                    PlayerIdentityName = x.PlayerSearch,
                    Team = new Team
                    {
                        TeamId = x.TeamId
                    }
                },
                Reason = x.Reason
            }).ToList();

            model.Match.UpdateMatchNameAutomatically = beforeUpdate.UpdateMatchNameAutomatically;
            model.Match.Teams = beforeUpdate.Teams;

            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditMatchResult] && ModelState.IsValid)
            {
                if (model.Match.MatchResultType.HasValue && (int)model.Match.MatchResultType.Value == -1) { model.Match.MatchResultType = null; }

                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var updatedMatch = await _matchRepository.UpdateCloseOfPlay(model.Match, currentMember.Key, currentMember.Name);
                await _cacheClearer.ClearCacheFor(updatedMatch);

                return Redirect(updatedMatch.MatchRoute);
            }

            model.Match.MatchName = beforeUpdate.MatchName;
            model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateTimeFormatter.FormatDate(x, false, false, false));

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
            model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.MatchName, Url = new Uri(model.Match.MatchRoute, UriKind.Relative) });

            return View("EditCloseOfPlay", model);
        }
    }
}