using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Caching;
using Stoolball.Competitions;
using Stoolball.Navigation;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Web.Security;
using Umbraco.Core.Cache;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using static Stoolball.Constants;

namespace Stoolball.Web.Competitions
{
    public class CreateCompetitionSurfaceController : SurfaceController
    {
        private readonly ICompetitionRepository _competitionRepository;
        private readonly IAuthorizationPolicy<Competition> _authorizationPolicy;
        private readonly IRouteGenerator _routeGenerator;
        private readonly ICacheOverride _cacheOverride;

        public CreateCompetitionSurfaceController(IUmbracoContextAccessor umbracoContextAccessor, IUmbracoDatabaseFactory umbracoDatabaseFactory, ServiceContext serviceContext,
            AppCaches appCaches, ILogger logger, IProfilingLogger profilingLogger, UmbracoHelper umbracoHelper, ICompetitionRepository seasonRepository,
            IAuthorizationPolicy<Competition> authorizationPolicy, IRouteGenerator routeGenerator, ICacheOverride cacheOverride)
            : base(umbracoContextAccessor, umbracoDatabaseFactory, serviceContext, appCaches, logger, profilingLogger, umbracoHelper)
        {
            _competitionRepository = seasonRepository ?? throw new System.ArgumentNullException(nameof(seasonRepository));
            _authorizationPolicy = authorizationPolicy ?? throw new System.ArgumentNullException(nameof(authorizationPolicy));
            _routeGenerator = routeGenerator ?? throw new System.ArgumentNullException(nameof(routeGenerator));
            _cacheOverride = cacheOverride ?? throw new ArgumentNullException(nameof(cacheOverride));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateUmbracoFormRouteString]
        [ContentSecurityPolicy(TinyMCE = true, Forms = true)]
        public async Task<ActionResult> CreateCompetition([Bind(Prefix = "Competition", Include = "CompetitionName,UntilYear,PlayerType,Facebook,Twitter,Instagram,YouTube,Website")] Competition competition)
        {
            if (competition is null)
            {
                throw new System.ArgumentNullException(nameof(competition));
            }

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            competition.Introduction = Request.Unvalidated.Form["Competition.Introduction"];
            competition.PublicContactDetails = Request.Unvalidated.Form["Competition.PublicContactDetails"];
            competition.PrivateContactDetails = Request.Unvalidated.Form["Competition.PrivateContactDetails"];

            var isAuthorized = _authorizationPolicy.IsAuthorized(competition);

            if (isAuthorized[AuthorizedAction.CreateCompetition] && ModelState.IsValid)
            {
                // Create an owner group
                var groupName = _routeGenerator.GenerateRoute("competition", competition.CompetitionName, NoiseWords.CompetitionRoute);
                IMemberGroup group;
                do
                {
                    group = Services.MemberGroupService.GetByName(groupName);
                    if (group == null)
                    {
                        group = new MemberGroup
                        {
                            Name = groupName
                        };
                        Services.MemberGroupService.Save(group);
                        competition.MemberGroupKey = group.Key;
                        competition.MemberGroupName = group.Name;
                        break;
                    }
                    else
                    {
                        groupName = _routeGenerator.IncrementRoute(groupName);
                    }
                }
                while (group != null);

                // Assign the current member to the group unless they're already admin
                var currentMember = Members.GetCurrentMember();
                if (!Members.IsMemberAuthorized(null, new[] { Groups.Administrators }, null))
                {
                    Services.MemberService.AssignRole(currentMember.Id, group.Name);
                }

                // Create the competition
                var createdCompetition = await _competitionRepository.CreateCompetition(competition, currentMember.Key, currentMember.Name).ConfigureAwait(false);
                _cacheOverride.OverrideCacheForCurrentMember(CacheConstants.CompetitionsPolicyCacheKeyPrefix);

                // Redirect to the competition
                return Redirect(createdCompetition.CompetitionRoute);
            }

            var viewModel = new CompetitionViewModel(CurrentPage, Services.UserService)
            {
                Competition = competition,
            };
            viewModel.IsAuthorized = isAuthorized;
            viewModel.Metadata.PageTitle = $"Add a competition";

            viewModel.Breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });

            return View("CreateCompetition", viewModel);
        }
    }
}