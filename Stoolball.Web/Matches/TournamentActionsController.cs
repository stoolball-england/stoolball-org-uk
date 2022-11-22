using System;
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
    public class TournamentActionsController : RenderController, IRenderControllerAsync
    {
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;

        public TournamentActionsController(ILogger<TournamentActionsController> logger,
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
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new TournamentViewModel(CurrentPage)
            {
                Tournament = await _tournamentDataSource.ReadTournamentByRoute(Request.Path)
            };

            if (model.Tournament == null)
            {
                return NotFound();
            }
            else
            {
                model.Authorization.AuthorizedAction = "edit or delete";
                model.Authorization.AuthorizationFor = model.Tournament.TournamentFullName(x => _dateFormatter.FormatDate(x, false, false, false));
                model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Tournament);
                model.Authorization.AuthorizedMemberNames = await _authorizationPolicy.AuthorizedMemberNames(model.Tournament);

                model.Metadata.PageTitle = model.Tournament.TournamentFullNameAndPlayerType(x => _dateFormatter.FormatDate(x, false, false, false));

                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Tournament.TournamentName, Url = new Uri(model.Tournament.TournamentRoute, UriKind.Relative) });

                return CurrentTemplate(model);
            }
        }
    }
}