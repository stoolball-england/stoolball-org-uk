﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Matches
{
    public class EditTournamentSeasonsSurfaceController : SurfaceController
    {
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly ITournamentRepository _tournamentRepository;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;

        public EditTournamentSeasonsSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ITournamentDataSource tournamentDataSource,
            ITournamentRepository tournamentRepository, IAuthorizationPolicy<Tournament> authorizationPolicy)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _tournamentDataSource = tournamentDataSource ?? throw new ArgumentNullException(nameof(tournamentDataSource));
            _tournamentRepository = tournamentRepository ?? throw new ArgumentNullException(nameof(tournamentRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateSeasons()
        {
            var beforeUpdate = await _tournamentDataSource.ReadTournamentByRoute(Request.RawUrl).ConfigureAwait(false);

            var model = new EditTournamentViewModel(CurrentPage, Services.UserService)
            {
                Tournament = beforeUpdate
            };
            model.Tournament.Seasons = Request.Form["Tournament.Seasons"]?.Split(',').Select(x => new Season { SeasonId = new Guid(x) }).ToList() ?? new List<Season>();

            model.IsAuthorized = _authorizationPolicy.IsAuthorized(model.Tournament);

            if (model.IsAuthorized[AuthorizedAction.EditTournament])
            {
                var currentMember = Members.GetCurrentMember();
                var updatedTournament = await _tournamentRepository.UpdateSeasons(model.Tournament, currentMember.Key, Members.CurrentUserName, currentMember.Name).ConfigureAwait(false);
            }

            // Redirect to the tournament actions page
            return Redirect(model.Tournament.TournamentRoute + "/edit");
        }
    }
}