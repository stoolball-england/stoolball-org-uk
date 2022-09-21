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
    public class CreateSeasonSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly ISeasonRepository _seasonRepository;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public CreateSeasonSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            ICompetitionDataSource competitionDataSource, ISeasonRepository seasonRepository, IAuthorizationPolicy<Competition> authorizationPolicy)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _seasonRepository = seasonRepository ?? throw new System.ArgumentNullException(nameof(seasonRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<ActionResult> CreateSeason([Bind("FromYear", "UntilYear", "EnableTournaments", "EnableBonusOrPenaltyRuns", "PlayersPerTeam", "DefaultOverSets", "EnableLastPlayerBatsOn", Prefix = "Season")] Season season)
        {
            if (season is null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            season.DefaultOverSets.RemoveAll(x => !x.Overs.HasValue);

            // end year is actually populated with the number of years to add to the start year,
            // because that allows the start year to be changed from the default without using JavaScript 
            // to update the value of the end year radio buttons
            season.UntilYear = season.FromYear + season.UntilYear;

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            season.Introduction = Request.Form["Season.Introduction"];
            season.Results = Request.Form["Season.Results"];

            try
            {
                // parse this because there's no way to get it via the standard modelbinder without requiring JavaScript to change the field names on submit
                var unparsedMatchTypes = Request.Form["Season.MatchTypes"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (unparsedMatchTypes.Any())
                {
                    season.MatchTypes = unparsedMatchTypes.Select(x => (MatchType)Enum.Parse(typeof(MatchType), x)).ToList() ?? new List<MatchType>();
                }
            }
            catch (InvalidCastException)
            {
                return StatusCode(400);
            }

            if (!season.MatchTypes.Any() && !season.EnableTournaments)
            {
                ModelState.AddModelError("Season.MatchTypes", $"Please select at least one type of match");
            }

            season.Competition = await _competitionDataSource.ReadCompetitionByRoute(Request.Path).ConfigureAwait(false);

            // Ensure there isn't already a season with the submitted year(s)
            if (season.Competition.Seasons.Any(x => x.FromYear == season.FromYear && x.UntilYear == season.UntilYear))
            {
                ModelState.AddModelError("Season.FromYear", $"There is already a {season.SeasonName()}");
            }

            var isAuthorized = await _authorizationPolicy.IsAuthorized(season.Competition);

            if (isAuthorized[AuthorizedAction.EditCompetition] && ModelState.IsValid)
            {
                // Create the season
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var createdSeason = await _seasonRepository.CreateSeason(season, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the season
                return Redirect(createdSeason.SeasonRoute);
            }

            var viewModel = new SeasonViewModel(CurrentPage, Services.UserService)
            {
                Season = season
            };
            if (!viewModel.Season.DefaultOverSets.Any())
            {
                viewModel.Season.DefaultOverSets.Add(new OverSet());
            }
            viewModel.Authorization.CurrentMemberIsAuthorized = isAuthorized;
            var the = season.Competition.CompetitionName.StartsWith("THE ", StringComparison.OrdinalIgnoreCase) ? string.Empty : "the ";
            viewModel.Metadata.PageTitle = $"Add a season in {the}{season.Competition.CompetitionName}";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = viewModel.Season.Competition.CompetitionName, Url = new Uri(viewModel.Season.Competition.CompetitionRoute, UriKind.Relative) });

            return View("CreateSeason", viewModel);
        }
    }
}