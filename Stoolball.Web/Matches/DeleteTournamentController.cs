using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Matches
{
    public class DeleteTournamentController : RenderController, IRenderControllerAsync
    {
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly ICommentsDataSource<Tournament> _tournamentCommentsDataSource;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;

        public DeleteTournamentController(ILogger<DeleteTournamentController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITournamentDataSource tournamentDataSource,
            IMatchListingDataSource matchDataSource,
            ICommentsDataSource<Tournament> tournamentCommentsDataSource,
            IAuthorizationPolicy<Tournament> authorizationPolicy,
            IDateTimeFormatter dateFormatter)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _tournamentDataSource = tournamentDataSource ?? throw new ArgumentNullException(nameof(tournamentDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _tournamentCommentsDataSource = tournamentCommentsDataSource ?? throw new ArgumentNullException(nameof(tournamentCommentsDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new DeleteTournamentViewModel(CurrentPage)
            {
                Tournament = await _tournamentDataSource.ReadTournamentByRoute(Request.Path),
            };

            if (model.Tournament == null)
            {
                return NotFound();
            }
            else
            {
                model.TotalComments = await _tournamentCommentsDataSource.ReadTotalComments(model.Tournament.TournamentId!.Value);

                model.Matches = new MatchListingViewModel(CurrentPage)
                {
                    Matches = await _matchDataSource.ReadMatchListings(new MatchFilter
                    {
                        TournamentId = model.Tournament.TournamentId,
                        IncludeTournamentMatches = true,
                        IncludeTournaments = false
                    }, MatchSortOrder.MatchDateEarliestFirst)
                };

                model.ConfirmDeleteRequest.RequiredText = model.Tournament.TournamentName;

                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Tournament);

                model.Metadata.PageTitle = "Delete " + model.Tournament.TournamentFullNameAndPlayerType(x => _dateFormatter.FormatDate(x, false, false, false));

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Tournament.TournamentName, Url = new Uri(model.Tournament.TournamentRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}