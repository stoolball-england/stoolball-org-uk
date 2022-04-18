using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Competitions
{
    public class MatchesForSeasonController : RenderController, IRenderControllerAsync
    {
        private readonly IMatchFilterFactory _matchFilterFactory;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly IAddMatchMenuViewModelFactory _addMatchMenuViewModelFactory;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public MatchesForSeasonController(ILogger<MatchesForSeasonController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMatchFilterFactory matchFilterFactory,
            ISeasonDataSource seasonDataSource,
            IMatchListingDataSource matchDataSource,
            IAddMatchMenuViewModelFactory addMatchMenuViewModelFactory,
            IAuthorizationPolicy<Competition> authorizationPolicy)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _matchFilterFactory = matchFilterFactory ?? throw new ArgumentNullException(nameof(matchFilterFactory));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _addMatchMenuViewModelFactory = addMatchMenuViewModelFactory ?? throw new ArgumentNullException(nameof(addMatchMenuViewModelFactory));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var season = await _seasonDataSource.ReadSeasonByRoute(Request.Path, false).ConfigureAwait(false);

            if (season == null)
            {
                return NotFound();
            }
            else
            {
                var filter = _matchFilterFactory.MatchesForSeason(season.SeasonId!.Value);
                var model = new SeasonViewModel(CurrentPage)
                {
                    Season = season,
                    Matches = new MatchListingViewModel(CurrentPage)
                    {
                        Matches = await _matchDataSource.ReadMatchListings(filter.filter, filter.sortOrder).ConfigureAwait(false)
                    },
                };
                if (model.Season.MatchTypes.Contains(MatchType.LeagueMatch) || model.Season.MatchTypes.Contains(MatchType.KnockoutMatch))
                {
                    model.Matches.MatchTypesToLabel.Add(MatchType.FriendlyMatch);
                }

                model.AddMatchMenu = _addMatchMenuViewModelFactory.CreateModel(model.Season.SeasonRoute, true,
                    model.Season.MatchTypes.Contains(MatchType.TrainingSession),
                    model.Season.MatchTypes.Contains(MatchType.FriendlyMatch),
                    model.Season.MatchTypes.Contains(MatchType.KnockoutMatch),
                    model.Season.MatchTypes.Contains(MatchType.LeagueMatch),
                    model.Season.EnableTournaments);

                model.IsAuthorized = await _authorizationPolicy.IsAuthorized(model.Season.Competition);

                model.Metadata.PageTitle = $"Matches and tournaments in {model.Season.SeasonFullNameAndPlayerType()}";

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.Competition.CompetitionName, Url = new Uri(model.Season.Competition.CompetitionRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}