using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Matches
{
    public class EditTournamentSeasonsController : RenderController, IRenderControllerAsync
    {
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly ISeasonEstimator _seasonEstimator;
        private readonly IDateTimeFormatter _dateFormatter;

        public EditTournamentSeasonsController(ILogger<EditTournamentSeasonsController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITournamentDataSource tournamentDataSource,
            ISeasonDataSource seasonDataSource,
            IAuthorizationPolicy<Tournament> authorizationPolicy,
            ISeasonEstimator seasonEstimator,
            IDateTimeFormatter dateFormatter)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _tournamentDataSource = tournamentDataSource ?? throw new ArgumentNullException(nameof(tournamentDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _seasonEstimator = seasonEstimator ?? throw new ArgumentNullException(nameof(seasonEstimator));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new EditTournamentViewModel(CurrentPage)
            {
                Tournament = await _tournamentDataSource.ReadTournamentByRoute(Request.Path),
                UrlReferrer = Request.GetTypedHeaders().Referer
            };

            if (model.Tournament == null)
            {
                return NotFound();
            }
            else
            {
                // By filtering for all match types it excludes seasons for annual tournaments like Expo and Seaford, which may support training sessions 
                // but otherwise no match types. Although those tournaments can be listed in other seasons (they're of interest to the league teams), we 
                // don't want other tournaments listed on pages which are supposed to be just about those annual tournaments. For any tournaments which 
                // actually are meant to be in those seasons, they will be added back when the tournament's current seasons are added to the list.
                var seasonDates = _seasonEstimator.EstimateSeasonDates(model.Tournament.StartTime);

                var filter = new CompetitionFilter
                {
                    FromYear = seasonDates.fromDate.Year,
                    UntilYear = seasonDates.untilDate.Year,
                    PlayerTypes = new List<PlayerType> { model.Tournament.PlayerType },
                    MatchTypes = new List<MatchType> { MatchType.FriendlyMatch, MatchType.KnockoutMatch, MatchType.LeagueMatch },
                    EnableTournaments = true
                };
                model.PossibleSeasons.AddRange(await _seasonDataSource.ReadSeasons(filter));
                foreach (var season in model.Tournament.Seasons)
                {
                    if (!model.PossibleSeasons.Select(x => x.SeasonId!.Value).Contains(season.SeasonId!.Value))
                    {
                        model.PossibleSeasons.Add(season);
                    }
                }
                model.PossibleSeasons.Sort(new SeasonComparer());

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Tournament);

                model.TournamentDate = model.Tournament.StartTime;
                model.Metadata.PageTitle = "Where to list " + model.Tournament.TournamentFullName(x => _dateFormatter.FormatDate(x, false, false, false));

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Tournament.TournamentName, Url = new Uri(model.Tournament.TournamentRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}