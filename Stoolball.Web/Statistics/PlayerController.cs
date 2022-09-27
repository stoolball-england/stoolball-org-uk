using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Statistics;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Statistics
{
    public class PlayerController : RenderController, IRenderControllerAsync
    {
        private readonly IPlayerSummaryViewModelFactory _viewModelFactory;
        private readonly IBestPerformanceInAMatchStatisticsDataSource _bestPerformanceDataSource;
        private readonly IMemberManager _memberManager;

        public PlayerController(ILogger<PlayerController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IPlayerSummaryViewModelFactory viewModelFactory,
            IBestPerformanceInAMatchStatisticsDataSource bestPerformanceDataSource,
            IMemberManager memberManager)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
            _bestPerformanceDataSource = bestPerformanceDataSource ?? throw new ArgumentNullException(nameof(bestPerformanceDataSource));
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = await _viewModelFactory.CreateViewModel(CurrentPage, Request.Path, Request.QueryString.Value);

            if (model.Player == null)
            {
                return NotFound();
            }
            else
            {
                var playerOfTheMatchFilter = model.AppliedFilter.Clone();
                playerOfTheMatchFilter.PlayerOfTheMatch = true;
                model.TotalPlayerOfTheMatchAwards = await _bestPerformanceDataSource.ReadTotalPlayerIdentityPerformances(playerOfTheMatchFilter);

                if (model.Player.MemberKey.HasValue)
                {
                    var currentMember = await _memberManager.GetCurrentMemberAsync();
                    model.IsCurrentMember = model.Player.MemberKey == currentMember?.Key;
                }

                if (Request.Query.ContainsKey(Constants.QueryParameters.ConfirmPlayerLinkedToMember))
                {
                    model.ShowPlayerLinkedToMemberConfirmation = true;
                }

                return CurrentTemplate(model);
            }
        }
    }
}