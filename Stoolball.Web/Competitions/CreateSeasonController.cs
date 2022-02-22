using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Competitions
{
    public class CreateSeasonController : RenderController, IRenderControllerAsync
    {
        private readonly ICompetitionDataSource _competitionDataSource;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;

        public CreateSeasonController(ILogger<CreateSeasonController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ICompetitionDataSource competitionDataSource,
            IAuthorizationPolicy<Competition> authorizationPolicy)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _competitionDataSource = competitionDataSource ?? throw new ArgumentNullException(nameof(competitionDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpGet]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public new async Task<IActionResult> Index()
        {
            var competition = await _competitionDataSource.ReadCompetitionByRoute(Request.Path).ConfigureAwait(false);

            if (competition == null)
            {
                return NotFound();
            }
            else
            {
                var model = new SeasonViewModel(CurrentPage)
                {
                    Season = competition.Seasons.FirstOrDefault() ?? new Season { PlayersPerTeam = 11 }
                };
                if (!model.Season.DefaultOverSets.Any())
                {
                    model.Season.DefaultOverSets.Add(new OverSet());
                }
                var summerSeason = model.Season.FromYear == model.Season.UntilYear;
                model.Season.Competition = competition;
                model.Season.FromYear = model.Season.FromYear == default ? DateTime.Today.Year : model.Season.FromYear + 1;
                model.Season.UntilYear = summerSeason ? 0 : 1;

                model.IsAuthorized = await _authorizationPolicy.IsAuthorized(model.Season.Competition);

                var the = model.Season.Competition.CompetitionName.StartsWith("THE ", StringComparison.OrdinalIgnoreCase) ? string.Empty : "the ";
                model.Metadata.PageTitle = $"Add a season in {the}{model.Season.Competition.CompetitionName}";

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Season.Competition.CompetitionName, Url = new Uri(model.Season.Competition.CompetitionRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}