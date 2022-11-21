using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
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
    public class EditKnockoutMatchSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMatchDataSource _matchDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IMatchRepository _matchRepository;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IEditMatchHelper _editMatchHelper;
        private readonly IMatchValidator _matchValidator;
        private readonly IMatchListingCacheInvalidator _cacheClearer;

        public EditKnockoutMatchSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IMatchDataSource matchDataSource, ISeasonDataSource seasonDataSource, IMatchRepository matchRepository, IAuthorizationPolicy<Match> authorizationPolicy,
            IDateTimeFormatter dateTimeFormatter, IEditMatchHelper editMatchHelper, IMatchValidator matchValidator, IMatchListingCacheInvalidator cacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _editMatchHelper = editMatchHelper ?? throw new ArgumentNullException(nameof(editMatchHelper));
            _matchValidator = matchValidator ?? throw new ArgumentNullException(nameof(matchValidator));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async Task<IActionResult> UpdateMatch([Bind("MatchResultType", Prefix = "Match")] Match postedMatch)
        {
            if (postedMatch is null)
            {
                throw new ArgumentNullException(nameof(postedMatch));
            }

            var beforeUpdate = await _matchDataSource.ReadMatchByRoute(Request.Path).ConfigureAwait(false);

            // This controller is only for matches in the future
            if (beforeUpdate.StartTime <= DateTime.UtcNow)
            {
                return NotFound();
            }

            var model = new EditKnockoutMatchViewModel(CurrentPage, Services.UserService)
            {
                Match = postedMatch
            };
            model.Match.MatchId = beforeUpdate.MatchId;
            model.Match.MatchRoute = beforeUpdate.MatchRoute;
            model.Match.UpdateMatchNameAutomatically = beforeUpdate.UpdateMatchNameAutomatically;
            model.Match.Season = beforeUpdate.Season;

            _editMatchHelper.ConfigureModelFromRequestData(model, Request.Form, ModelState);

            _matchValidator.DateIsValidForSqlServer(model.MatchDate, ModelState, "MatchDate", "match");
            _matchValidator.DateIsWithinTheSeason(model.MatchDate, model.Match.Season, ModelState, "MatchDate", "match");
            _matchValidator.TeamsMustBeDifferent(model, ModelState);

            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditMatch] && ModelState.IsValid)
            {
                if ((int?)model.Match.MatchResultType == -1) { model.Match.MatchResultType = null; }

                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var updatedMatch = await _matchRepository.UpdateMatch(model.Match, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                await _cacheClearer.InvalidateCacheForMatch(beforeUpdate, updatedMatch).ConfigureAwait(false);

                return Redirect(updatedMatch.MatchRoute);
            }

            model.Match.Season = model.Season = await _seasonDataSource.ReadSeasonByRoute(model.Match.Season.SeasonRoute, true).ConfigureAwait(false);
            model.PossibleSeasons = _editMatchHelper.PossibleSeasonsAsListItems(new[] { model.Match.Season });
            model.PossibleHomeTeams = _editMatchHelper.PossibleTeamsAsListItems(model.Season.Teams);
            model.PossibleAwayTeams = _editMatchHelper.PossibleTeamsAsListItems(model.Season.Teams);
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

            return View("EditKnockoutMatch", model);
        }
    }
}