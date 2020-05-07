using Stoolball.Dates;
using Stoolball.Email;
using Stoolball.Matches;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Routing;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Teams
{
    public class TransientTeamController : RenderMvcControllerAsync
    {
        private readonly ITeamDataSource _teamDataSource;
        private readonly IMatchDataSource _matchDataSource;
        private readonly IDateTimeFormatter _dateFormatter;
        private readonly IEmailProtector _emailProtector;

        public TransientTeamController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITeamDataSource teamDataSource,
           IMatchDataSource matchDataSource,
           IDateTimeFormatter dateFormatter,
           IEmailProtector emailProtector)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _teamDataSource = teamDataSource ?? throw new System.ArgumentNullException(nameof(teamDataSource));
            _matchDataSource = matchDataSource ?? throw new System.ArgumentNullException(nameof(matchDataSource));
            _dateFormatter = dateFormatter ?? throw new System.ArgumentNullException(nameof(dateFormatter));
            _emailProtector = emailProtector ?? throw new System.ArgumentNullException(nameof(emailProtector));
        }

        [HttpGet]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new TeamViewModel(contentModel.Content)
            {
                Team = await _teamDataSource.ReadTeamByRoute(Request.Url.AbsolutePath, true).ConfigureAwait(false)
            };

            if (model.Team == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.Matches = new MatchListingViewModel
                {
                    Matches = await _matchDataSource.ReadMatchListings(new MatchQuery
                    {
                        TeamIds = new List<Guid> { model.Team.TeamId.Value },
                        MatchTypes = new List<MatchType> { MatchType.Tournament }
                    }).ConfigureAwait(false),
                    DateTimeFormatter = _dateFormatter
                };

                model.Metadata.PageTitle = model.Team.TeamName + " stoolball team, " + _dateFormatter.FormatDate(model.Team.UntilDate.Value.Date, false, false);

                model.Team.Cost = _emailProtector.ProtectEmailAddresses(model.Team.Cost, User.Identity.IsAuthenticated);
                model.Team.Introduction = _emailProtector.ProtectEmailAddresses(model.Team.Introduction, User.Identity.IsAuthenticated);
                model.Team.PublicContactDetails = _emailProtector.ProtectEmailAddresses(model.Team.PublicContactDetails, User.Identity.IsAuthenticated);

                return CurrentTemplate(model);
            }
        }
    }
}