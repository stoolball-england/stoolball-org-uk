using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Statistics
{
    public class LinkPlayerToMemberController : RenderController, IRenderControllerAsync
    {
        private readonly IPlayerSummaryViewModelFactory _viewModelFactory;

        public LinkPlayerToMemberController(ILogger<PlayerController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IPlayerSummaryViewModelFactory viewModelFactory)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _viewModelFactory = viewModelFactory ?? throw new ArgumentNullException(nameof(viewModelFactory));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = await _viewModelFactory.CreateViewModel(CurrentPage, Request.Path, Request.QueryString.Value);

            if (model.Player == null || model.Player.MemberKey.HasValue)
            {
                return NotFound();
            }
            else
            {
                return CurrentTemplate(model);
            }
        }
    }
}