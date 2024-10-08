﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Navigation;
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
    public class EditTournamentTeamsSurfaceController : SurfaceController
    {
        private readonly IMemberManager _memberManager;
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly IMatchListingCacheInvalidator _cacheClearer;
        private readonly ITournamentRepository _tournamentRepository;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IPostSaveRedirector _postSaveRedirector;

        public EditTournamentTeamsSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, IProfilingLogger profilingLogger, IPublishedUrlProvider publishedUrlProvider, IMemberManager memberManager,
            ITournamentDataSource tournamentDataSource, ITournamentRepository tournamentRepository, IMatchListingCacheInvalidator cacheClearer,
            IAuthorizationPolicy<Tournament> authorizationPolicy, IDateTimeFormatter dateTimeFormatter, IPostSaveRedirector postSaveRedirector)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, profilingLogger, publishedUrlProvider)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _tournamentDataSource = tournamentDataSource ?? throw new ArgumentNullException(nameof(tournamentDataSource));
            _cacheClearer = cacheClearer ?? throw new ArgumentNullException(nameof(cacheClearer));
            _tournamentRepository = tournamentRepository ?? throw new ArgumentNullException(nameof(tournamentRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _postSaveRedirector = postSaveRedirector ?? throw new ArgumentNullException(nameof(postSaveRedirector));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<IActionResult> UpdateTeams([Bind("MaximumTeamsInTournament", "Teams", Prefix = "Tournament")] Tournament postedTournament)
        {
            if (postedTournament is null)
            {
                throw new ArgumentNullException(nameof(postedTournament));
            }

            var beforeUpdate = await _tournamentDataSource.ReadTournamentByRoute(Request.Path).ConfigureAwait(false);

            var model = new EditTournamentViewModel(CurrentPage, Services.UserService)
            {
                Tournament = postedTournament
            };
            model.Tournament.TournamentId = beforeUpdate.TournamentId;
            model.Tournament.TournamentName = beforeUpdate.TournamentName;
            model.Tournament.TournamentRoute = beforeUpdate.TournamentRoute;
            model.Tournament.StartTime = beforeUpdate.StartTime;
            model.Tournament.PlayerType = beforeUpdate.PlayerType;
            model.Tournament.TournamentLocation = beforeUpdate.TournamentLocation;

            // We're not interested in validating other details of the tournament or the selected teams
            foreach (var key in ModelState.Keys.Where(x => x != "Tournament.MaximumTeamsInTournament"))
            {
                ModelState.Remove(key);
            }

            model.Authorization.CurrentMemberIsAuthorized = await _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTournament] && ModelState.IsValid)
            {
                var currentMember = await _memberManager.GetCurrentMemberAsync();
                var updatedTournament = await _tournamentRepository.UpdateTeams(model.Tournament, currentMember.Key, currentMember.UserName, currentMember.Name).ConfigureAwait(false);
                await _cacheClearer.InvalidateCacheForTournament(beforeUpdate, updatedTournament).ConfigureAwait(false);

                return _postSaveRedirector.WorkOutRedirect(model.Tournament.TournamentRoute, updatedTournament.TournamentRoute, "/edit", Request.Form["UrlReferrer"], null);
            }

            model.Metadata.PageTitle = "Teams in the " + model.Tournament.TournamentFullName(x => _dateTimeFormatter.FormatDate(x, false, false, false));

            model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Tournaments, Url = new Uri(Constants.Pages.TournamentsUrl, UriKind.Relative) });
            model.Breadcrumbs.Add(new Breadcrumb { Name = model.Tournament.TournamentName, Url = new Uri(model.Tournament.TournamentRoute, UriKind.Relative) });

            return View("EditTournamentTeams", model);
        }
    }
}