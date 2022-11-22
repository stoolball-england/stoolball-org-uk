using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Matches
{
    public class EditTournamentController : RenderController, IRenderControllerAsync
    {
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;

        public EditTournamentController(ILogger<EditTournamentController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            ITournamentDataSource tournamentDataSource,
            IAuthorizationPolicy<Tournament> authorizationPolicy,
            IDateTimeFormatter dateFormatter)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _tournamentDataSource = tournamentDataSource ?? throw new ArgumentNullException(nameof(tournamentDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async new Task<IActionResult> Index()
        {
            var model = new EditTournamentViewModel(CurrentPage)
            {
                Tournament = await _tournamentDataSource.ReadTournamentByRoute(Request.Path)
            };

            if (model.Tournament == null)
            {
                return NotFound();
            }
            else
            {
                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Tournament);

                model.TournamentDate = model.Tournament.StartTime;
                if (model.Tournament.StartTimeIsKnown)
                {
                    model.StartTime = model.Tournament.StartTime;
                }
                model.TournamentLocationId = model.Tournament.TournamentLocation?.MatchLocationId;
                model.TournamentLocationName = model.Tournament.TournamentLocation?.NameAndLocalityOrTownIfDifferent();
                if (!model.Tournament.DefaultOverSets.Any())
                {
                    model.Tournament.DefaultOverSets.Add(new OverSet());
                }

                model.Metadata.PageTitle = "Edit " + model.Tournament.TournamentFullName(x => _dateFormatter.FormatDate(x, false, false, false));

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Tournament.TournamentName, Url = new Uri(model.Tournament.TournamentRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}