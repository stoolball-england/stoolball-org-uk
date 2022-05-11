using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Competitions.Models;
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

namespace Stoolball.Web.Competitions
{
    public class EditSeasonSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public EditSeasonSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager, ISeasonDataSource seasonDataSource,
            ISeasonRepository seasonRepository, IAuthorizationPolicy<Competition> authorizationPolicy)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _seasonRepository = seasonRepository ?? throw new ArgumentNullException(nameof(seasonRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<ActionResult> UpdateSeason([Bind("EnableTournaments", "PlayersPerTeam", "DefaultOverSets", "EnableLastPlayerBatsOn", "EnableBonusOrPenaltyRuns", Prefix = "Season")] Season season)
        {
            if (season is null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            var beforeUpdate = await _seasonDataSource.ReadSeasonByRoute(Request.Path).ConfigureAwait(false);

            season.DefaultOverSets.RemoveAll(x => !x.Overs.HasValue);

            // get this from the form instead of via modelbinding so that HTML can be allowed
            season.SeasonId = beforeUpdate.SeasonId;
            season.Introduction = Request.Form["Season.Introduction"];
            season.Results = Request.Form["Season.Results"];

            try
            {
                // parse this because there's no way to get it via the standard modelbinder without requiring JavaScript to change the field names on submit
                season.MatchTypes = Request.Form["Season.MatchTypes"].ToString().Split(',').Select(x => (MatchType)Enum.Parse(typeof(MatchType), x)).ToList() ?? new List<MatchType>();
            }
            catch (InvalidCastException)
            {
                return new StatusCodeResult(400);
            }

            if (!season.MatchTypes.Any() && !season.EnableTournaments)
            {
                ModelState.AddModelError("Season.MatchTypes", $"Please select at least one type of match");
            }

            var isAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate.Competition);

            if (isAuthorized[AuthorizedAction.EditCompetition] && ModelState.IsValid)
            {
                // Update the season
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                await _seasonRepository.UpdateSeason(season, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the season actions page that led here
                return Redirect(beforeUpdate.SeasonRoute + "/edit");
            }

            var viewModel = new SeasonViewModel(CurrentPage, Services.UserService)
            {
                Season = beforeUpdate,
            };
            viewModel.Authorization.CurrentMemberIsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Edit {beforeUpdate.SeasonFullNameAndPlayerType()}";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Season.Competition.CompetitionName, Url = new Uri(viewModel.Season.Competition.CompetitionRoute, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Season.SeasonName(), Url = new Uri(viewModel.Season.SeasonRoute, UriKind.Relative) });

            return View("EditSeason", viewModel);
        }
    }
}