using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
using Stoolball.Security;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Matches
{
    public class EditTournamentController : RenderMvcControllerAsync
    {
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;

        public EditTournamentController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITournamentDataSource tournamentDataSource,
           IAuthorizationPolicy<Tournament> authorizationPolicy,
           IDateTimeFormatter dateFormatter)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _tournamentDataSource = tournamentDataSource ?? throw new ArgumentNullException(nameof(tournamentDataSource));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new ArgumentNullException(nameof(contentModel));
            }

            var model = new EditTournamentViewModel(contentModel.Content, Services?.UserService)
            {
                Tournament = await _tournamentDataSource.ReadTournamentByRoute(Request.RawUrl).ConfigureAwait(false),
                DateFormatter = _dateFormatter
            };

            if (model.Tournament == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Tournament);

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