﻿using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models;

namespace Stoolball.Web.Matches
{
    public class DeleteTournamentController : RenderMvcControllerAsync
    {
        private readonly ITournamentDataSource _tournamentDataSource;
        private readonly IMatchListingDataSource _matchDataSource;
        private readonly ICommentsDataSource<Tournament> _tournamentCommentsDataSource;
        private readonly IAuthorizationPolicy<Tournament> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateFormatter;

        public DeleteTournamentController(IGlobalSettings globalSettings,
           IUmbracoContextAccessor umbracoContextAccessor,
           ServiceContext serviceContext,
           AppCaches appCaches,
           IProfilingLogger profilingLogger,
           UmbracoHelper umbracoHelper,
           ITournamentDataSource tournamentDataSource,
           IMatchListingDataSource matchDataSource,
           ICommentsDataSource<Tournament> tournamentCommentsDataSource,
           IAuthorizationPolicy<Tournament> authorizationPolicy,
           IDateTimeFormatter dateFormatter)
           : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper)
        {
            _tournamentDataSource = tournamentDataSource ?? throw new System.ArgumentNullException(nameof(tournamentDataSource));
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _tournamentCommentsDataSource = tournamentCommentsDataSource;
            _authorizationPolicy = authorizationPolicy;
            _dateFormatter = dateFormatter ?? throw new ArgumentNullException(nameof(dateFormatter));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true)]
        public async override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new DeleteTournamentViewModel(contentModel.Content)
            {
                Tournament = await _tournamentDataSource.ReadTournamentByRoute(Request.Url.AbsolutePath).ConfigureAwait(false),
                DateTimeFormatter = _dateFormatter
            };

            if (model.Tournament == null)
            {
                return new HttpNotFoundResult();
            }
            else
            {
                model.TotalComments = await _tournamentCommentsDataSource.ReadTotalComments(model.Tournament.TournamentId.Value).ConfigureAwait(false);

                model.Matches = new MatchListingViewModel
                {
                    Matches = await _matchDataSource.ReadMatchListings(new MatchQuery
                    {
                        TournamentId = model.Tournament.TournamentId,
                        IncludeTournamentMatches = true,
                        IncludeTournaments = false
                    }).ConfigureAwait(false)
                };

                model.ConfirmDeleteRequest.RequiredText = model.Tournament.TournamentName;

                model.IsAuthorized = IsAuthorized(model.Tournament);

                model.Metadata.PageTitle = "Delete " + model.Tournament.TournamentFullNameAndPlayerType(x => _dateFormatter.FormatDate(x.LocalDateTime, false, false, false));

                return CurrentTemplate(model);
            }
        }

        /// <summary>
        /// Checks whether the currently signed-in member is authorized to delete this tournament
        /// </summary>
        /// <returns></returns>
        protected virtual bool IsAuthorized(Tournament tournament)
        {
            return _authorizationPolicy.CanDelete(tournament, Members);
        }
    }
}