using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Web.Security;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Matches
{
    public class EditKnockoutMatchSurfaceController : SurfaceController
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly ISeasonDataSource _seasonDataSource;
        private readonly IMatchRepository _matchRepository;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IEditMatchHelper _editMatchHelper;

        public EditKnockoutMatchSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IMatchDataSource matchDataSource, ISeasonDataSource seasonDataSource,
            IMatchRepository matchRepository, IAuthorizationPolicy<Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter, IEditMatchHelper editMatchHelper)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _editMatchHelper = editMatchHelper ?? throw new ArgumentNullException(nameof(editMatchHelper));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true)]
        public async Task<ActionResult> UpdateMatch([Bind(Prefix = "Match", Include = "MatchResultType")] Match postedMatch)
        {
            if (postedMatch is null)
            {
                throw new ArgumentNullException(nameof(postedMatch));
            }

            var beforeUpdate = await _matchDataSource.ReadMatchByRoute(Request.RawUrl).ConfigureAwait(false);

            var model = new EditKnockoutMatchViewModel(CurrentPage)
            {
                Match = postedMatch,
                DateFormatter = _dateTimeFormatter
            };
            model.Match.MatchId = beforeUpdate.MatchId;
            model.Match.MatchRoute = beforeUpdate.MatchRoute;
            model.Match.UpdateMatchNameAutomatically = beforeUpdate.UpdateMatchNameAutomatically;
            model.Match.Season = beforeUpdate.Season;

            _editMatchHelper.ConfigureModelFromRequestData(model, Request.Unvalidated.Form, Request.Form);

            model.IsAuthorized = IsAuthorized(beforeUpdate);

            if (model.IsAuthorized && ModelState.IsValid)
            {
                if ((int)model.Match.MatchResultType == -1) { model.Match.MatchResultType = null; }

                var currentMember = Members.GetCurrentMember();
                await _matchRepository.UpdateMatch(model.Match, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                // Redirect to the match
                return Redirect(model.Match.MatchRoute);
            }

            model.Match.Season = model.Season = await _seasonDataSource.ReadSeasonByRoute(model.Match.Season.SeasonRoute, true).ConfigureAwait(false);
            model.PossibleSeasons = _editMatchHelper.PossibleSeasonsAsListItems(new[] { model.Match.Season });
            model.PossibleTeams = _editMatchHelper.PossibleTeamsAsListItems(model.Season.Teams);
            model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateTimeFormatter.FormatDate(x.LocalDateTime, false, false, false));

            return View("EditKnockoutMatch", model);
        }

        protected virtual bool IsAuthorized(Match match)
        {
            return _authorizationPolicy.CanEdit(match, Members);
        }
    }
}