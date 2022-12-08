using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Email;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Competitions
{
    public class SeasonResultsTableController : RenderController, IRenderControllerAsync
    {
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;
        private readonly IEmailProtector _emailProtector;

        public SeasonResultsTableController(ILogger<SeasonResultsTableController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ISeasonDataSource seasonDataSource,
            IMatchListingDataSource matchDataSource,
            IAuthorizationPolicy<Competition> authorizationPolicy,
            IEmailProtector emailProtector)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _emailProtector = emailProtector ?? throw new ArgumentNullException(nameof(emailProtector));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new SeasonViewModel(CurrentPage)
            {
                Season = await _seasonDataSource.ReadSeasonByRoute(Request.Path, true).ConfigureAwait(false)
            };

            if (model.Season == null || (!model.Season.MatchTypes.Contains(MatchType.LeagueMatch) &&
                !model.Season.MatchTypes.Contains(MatchType.KnockoutMatch) &&
                !model.Season.MatchTypes.Contains(MatchType.FriendlyMatch) &&
                string.IsNullOrEmpty(model.Season.Results)))
            {
                return NotFound();
            }
            else
            {
                model.Matches = new MatchListingViewModel(CurrentPage)
                {
                    Matches = await _matchDataSource.ReadMatchListings(new MatchFilter
                    {
                        SeasonIds = new List<Guid> { model.Season.SeasonId!.Value },
                        IncludeTournaments = false
                    }, MatchSortOrder.MatchDateEarliestFirst).ConfigureAwait(false)
                };
                model.Season.PointsRules.AddRange(await _seasonDataSource.ReadPointsRules(model.Season.SeasonId.Value).ConfigureAwait(false));
                model.Season.PointsAdjustments.AddRange(await _seasonDataSource.ReadPointsAdjustments(model.Season.SeasonId.Value).ConfigureAwait(false));

                model.Season.Results = _emailProtector.ProtectEmailAddresses(model.Season.Results, User.Identity?.IsAuthenticated ?? false);

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Season.Competition);

                var the = model.Season.Competition.CompetitionName.StartsWith("THE ", StringComparison.OrdinalIgnoreCase);
                model.Metadata.PageTitle = $"Results table for {(the ? string.Empty : "the ")}{model.Season.SeasonFullNameAndPlayerType()}";
                model.Metadata.Description = model.Season.Description();

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.Competition.CompetitionName, Url = new Uri(model.Season.Competition.CompetitionRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}