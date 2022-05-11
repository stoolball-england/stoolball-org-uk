using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Stoolball.Caching;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Routing;
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

namespace Stoolball.Web.Matches
{
    public class EditTournamentSeasonsSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly ICacheClearer<Tournament> _cacheClearer;
        private readonly ITournamentRepository _tournamentRepository;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IPostSaveRedirector _postSaveRedirector;

        public EditTournamentSeasonsSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            ITournamentDataSource tournamentDataSource, ITournamentRepository tournamentRepository, ICacheClearer<Tournament> cacheClearer,
            IAuthorizationPolicy<Tournament> authorizationPolicy, IPostSaveRedirector postSaveRedirector)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _tournamentDataSource = tournamentDataSource ?? throw new ArgumentNullException(nameof(tournamentDataSource));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
            _tournamentRepository = tournamentRepository ?? throw new ArgumentNullException(nameof(tournamentRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _postSaveRedirector = postSaveRedirector ?? throw new ArgumentNullException(nameof(postSaveRedirector));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateSeasons()
        {
            var beforeUpdate = await _tournamentDataSource.ReadTournamentByRoute(Request.Path);

            var model = new EditTournamentViewModel(CurrentPage, Services.UserService)
            {
                Tournament = beforeUpdate
            };
            if (Request.Form["Tournament.Seasons"] != StringValues.Empty)
            {
                model.Tournament.Seasons = Request.Form["Tournament.Seasons"].ToString().Split(',').Select(x => new Season { SeasonId = new Guid(x) }).ToList() ?? new List<Season>();
            }

            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(model.Tournament);

            if (model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTournament])
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var updatedTournament = await _tournamentRepository.UpdateSeasons(model.Tournament, currentMember.Key, currentMember.UserName, currentMember.Name);
                await _cacheClearer.ClearCacheFor(updatedTournament);
            }

            return _postSaveRedirector.WorkOutRedirect(model.Tournament.TournamentRoute, model.Tournament.TournamentRoute, "/edit", Request.Form["UrlReferrer"], null);
        }
    }
}