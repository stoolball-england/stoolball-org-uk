using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Navigation;
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
    public class EditMatchFormatSurfaceController : SurfaceController
    {
        private readonly IMatchDataSource _matchDataSource;
        private readonly IMatchRepository _matchRepository;
        private readonly IAuthorizationPolicy<Match> _authorizationPolicy;
        private readonly IDateTimeFormatter _dateTimeFormatter;
        private readonly IMatchInningsFactory _matchInningsFactory;
        private readonly ISeasonDataSource _seasonDataSource;

        public EditMatchFormatSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, IMatchDataSource matchDataSource,
            IMatchRepository matchRepository, IAuthorizationPolicy<Match> authorizationPolicy, IDateTimeFormatter dateTimeFormatter, IMatchInningsFactory matchInningsFactory,
            ISeasonDataSource seasonDataSource)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _matchDataSource = matchDataSource ?? throw new ArgumentNullException(nameof(matchDataSource));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new ArgumentNullException(nameof(authorizationPolicy));
            _dateTimeFormatter = dateTimeFormatter ?? throw new ArgumentNullException(nameof(dateTimeFormatter));
            _matchInningsFactory = matchInningsFactory ?? throw new ArgumentNullException(nameof(matchInningsFactory));
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(Forms = true)]
        public async Task<ActionResult> UpdateMatch([Bind(Prefix = "FormData")] EditMatchFormatFormData postedData)
        {
            if (postedData is null)
            {
                throw new ArgumentNullException(nameof(postedData));
            }

            var beforeUpdate = await _matchDataSource.ReadMatchByRoute(Request.RawUrl).ConfigureAwait(false);

            if (beforeUpdate == null || beforeUpdate.Tournament != null || beforeUpdate.MatchType == MatchType.TrainingSession)
            {
                return new HttpNotFoundResult();
            }

            var model = new EditMatchFormatViewModel(CurrentPage, Services.UserService)
            {
                Match = beforeUpdate,
                DateFormatter = _dateTimeFormatter,
                FormData = postedData
            };

            var matchIsInTheFuture = model.Match.StartTime > DateTimeOffset.UtcNow;
            if (postedData.MatchInnings < 2 || postedData.MatchInnings % 2 != 0)
            {
                ModelState.AddModelError("FormData.MatchInnings", $"A match cannot have an odd number of innings or less than 2 innings.");
            }
            else if (!matchIsInTheFuture && postedData.MatchInnings < model.Match.MatchInnings.Count)
            {
                ModelState.AddModelError("FormData.MatchInnings", $"You cannot reduce the number of innings after a match has happened.");
            }
            else
            {

                while (matchIsInTheFuture && postedData.MatchInnings < model.Match.MatchInnings.Count)
                {
                    model.Match.MatchInnings.RemoveAt(model.Match.MatchInnings.Count - 1);
                }

                if (postedData.MatchInnings > model.Match.MatchInnings.Count)
                {
                    // Get potential default oversets
                    if (model.Match.Season != null)
                    {
                        model.Match.Season = await _seasonDataSource.ReadSeasonById(model.Match.Season.SeasonId.Value, true).ConfigureAwait(false);
                    }

                    while (postedData.MatchInnings > model.Match.MatchInnings.Count)
                    {
                        var battingMatchTeamId = model.Match.MatchInnings.Count % 2 == 0 ? model.Match.MatchInnings[0].BattingMatchTeamId : model.Match.MatchInnings[1].BattingMatchTeamId;
                        var bowlingMatchTeamId = model.Match.MatchInnings.Count % 2 == 1 ? model.Match.MatchInnings[0].BattingMatchTeamId : model.Match.MatchInnings[1].BattingMatchTeamId;
                        model.Match.MatchInnings.Add(_matchInningsFactory.CreateMatchInnings(model.Match, battingMatchTeamId, bowlingMatchTeamId));
                    }
                }

                foreach (var innings in model.Match.MatchInnings)
                {
                    foreach (var overSet in innings.OverSets)
                    {
                        if (!matchIsInTheFuture && postedData.Overs < overSet.Overs)
                        {
                            ModelState.AddModelError("FormData.Overs", $"You cannot reduce the number of overs after a match has happened.");
                        }
                        else
                        {
                            overSet.Overs = postedData.Overs;
                        }
                    }
                }
            }

            model.IsAuthorized = _authorizationPolicy.IsAuthorized(beforeUpdate);

            if (model.IsAuthorized[AuthorizedAction.EditMatch] && ModelState.IsValid)
            {
                var currentMember = Members.GetCurrentMember();
                var updatedMatch = await _matchRepository.UpdateMatchFormat(model.Match, currentMember.Key, currentMember.Name).ConfigureAwait(false);

                return Redirect(updatedMatch.MatchRoute + "/edit");
            }

            model.Metadata.PageTitle = "Edit " + model.Match.MatchFullName(x => _dateTimeFormatter.FormatDate(x, false, false, false));

            if (model.Match.Season != null)
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Season.Competition.CompetitionName, Url = new Uri(model.Match.Season.Competition.CompetitionRoute, UriKind.Relative) });
                model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.Season.SeasonName(), Url = new Uri(model.Match.Season.SeasonRoute, UriKind.Relative) });
            }
            else
            {
                model.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Matches, Url = new Uri(Constants.Pages.MatchesUrl, UriKind.Relative) });
            }
            model.Breadcrumbs.Add(new Breadcrumb { Name = model.Match.MatchName, Url = new Uri(model.Match.MatchRoute, UriKind.Relative) });

            return View("EditMatchFormat", model);
        }
    }
}