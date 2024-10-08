﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Navigation;
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
    public class CreateFriendlyMatchSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly IMatchRepository _matchRepository;
        private readonly ITeamDataSource _teamDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ICreateMatchSeasonSelector _createMatchSeasonSelector;
        private readonly IEditMatchHelper _editMatchHelper;
        private readonly IMatchValidator _matchValidator;
        private readonly IMatchListingCacheInvalidator _cacheClearer;

        public CreateFriendlyMatchSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            IMatchRepository matchRepository, ITeamDataSource teamDataSource, ISeasonDataSource seasonDataSource,
            ICreateMatchSeasonSelector createMatchSeasonSelector, IEditMatchHelper editMatchHelper, IMatchValidator matchValidator, IMatchListingCacheInvalidator cacheClearer)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _teamDataSource = teamDataSource ?? throw new ArgumentNullException(nameof(teamDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _createMatchSeasonSelector = createMatchSeasonSelector ?? throw new ArgumentNullException(nameof(createMatchSeasonSelector));
            _editMatchHelper = editMatchHelper ?? throw new ArgumentNullException(nameof(editMatchHelper));
            _matchValidator = matchValidator ?? throw new ArgumentNullException(nameof(matchValidator));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async Task<IActionResult> CreateMatch([Bind("Season", Prefix = "Match")] Match postedMatch)
        {
            if (postedMatch is null)
            {
                throw new ArgumentNullException(nameof(postedMatch));
            }

            var model = new EditFriendlyMatchViewModel(CurrentPage, Services.UserService) { Match = postedMatch };
            model.Match.MatchType = MatchType.FriendlyMatch;
            _editMatchHelper.ConfigureModelFromRequestData(model, Request.Form, ModelState);

            var path = Request.Path.HasValue ? Request.Path.Value : string.Empty;
            if (path!.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                model.Season = model.Match.Season = await _seasonDataSource.ReadSeasonByRoute(Request.Path, false).ConfigureAwait(false);
            }
            else if (model.Match.Season != null && !model.Match.Season.SeasonId.HasValue)
            {
                model.Match.Season = null;
            }
            else if (model.Match.Season != null)
            {
                // Get the season, to support validation against season dates
                model.Match.Season = await _seasonDataSource.ReadSeasonById(model.Match.Season.SeasonId!.Value).ConfigureAwait(false);
            }

            _matchValidator.DateIsValidForSqlServer(model.MatchDate, ModelState, "MatchDate", "match");
            _matchValidator.DateIsWithinTheSeason(model.MatchDate, model.Match.Season, ModelState, "MatchDate", "match");
            _matchValidator.AtLeastOneTeamId(model, ModelState);

            model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateMatch] = User.Identity?.IsAuthenticated ?? false;

            if (model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateMatch] && ModelState.IsValid &&
                (model.Season == null || model.Season.MatchTypes.Contains(MatchType.FriendlyMatch)))
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var createdMatch = await _matchRepository.CreateMatch(model.Match, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                await _cacheClearer.InvalidateCacheForMatch(createdMatch).ConfigureAwait(false);

                if (Request.Form["AddAnother"].Any())
                {
                    return Redirect(path + "?confirm=" + Uri.EscapeUriString(createdMatch.MatchRoute));
                }
                else
                {
                    return Redirect(createdMatch.MatchRoute);
                }
            }

            if (path.StartsWith("/teams/", StringComparison.OrdinalIgnoreCase))
            {
                model.Team = await _teamDataSource.ReadTeamByRoute(Request.Path, false).ConfigureAwait(false);
                var possibleSeasons = _createMatchSeasonSelector.SelectPossibleSeasons(model.Team.Seasons, model.Match.MatchType);
                model.PossibleSeasons = _editMatchHelper.PossibleSeasonsAsListItems(possibleSeasons);
                model.Metadata.PageTitle = $"Add a {MatchType.FriendlyMatch.Humanize(LetterCasing.LowerCase)} for {model.Team.TeamName}";
            }
            else if (path.StartsWith("/competitions/", StringComparison.OrdinalIgnoreCase))
            {
                model.Metadata.PageTitle = $"Add a {MatchType.FriendlyMatch.Humanize(LetterCasing.LowerCase)} in the {model.Season!.SeasonFullName()}";
            }

            if (!string.IsNullOrEmpty(Request.Form["HomeTeamName"]))
            {
                model.HomeTeamName = Request.Form["HomeTeamName"];
            }
            if (!string.IsNullOrEmpty(Request.Form["AwayTeamName"]))
            {
                model.AwayTeamName = Request.Form["AwayTeamName"];
            }

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Matches, Url = new Uri(Constants.Pages.MatchesUrl, UriKind.Relative) });

            return View("CreateFriendlyMatch", model);
        }
    }
}