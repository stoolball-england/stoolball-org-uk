using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Data.Abstractions;
using Stoolball.Web.Navigation;
using Stoolball.Web.Routing;
using Stoolball.Web.Security;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Statistics
{
    public class LinkedPlayersForMemberController : RenderController, IRenderControllerAsync
    {
        private readonly IMemberManager _memberManager;
        private readonly IPlayerDataSource _playerDataSource;

        public LinkedPlayersForMemberController(ILogger<LinkedPlayersForMemberController> logger,
            ICompositeViewEngine compositeViewEngine,
            IUmbracoContextAccessor umbracoContextAccessor,
            IMemberManager memberManager,
            IPlayerDataSource playerDataSource)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
        }

        [HttpGet]
        [ContentSecurityPolicy]
        public async new Task<IActionResult> Index()
        {
            var model = new LinkedPlayersViewModel(CurrentPage);

            var member = await _memberManager.GetCurrentMemberAsync();
            if (member != null)
            {
                model.Player = await _playerDataSource.ReadPlayerByMemberKey(member.Key);
                if (model.Player != null)
                {
                    model.Player = await _playerDataSource.ReadPlayerByRoute(model.Player.PlayerRoute!);
                }
            }

            model.Metadata.PageTitle = "Players added to my statistics";
            model.Breadcrumbs.RemoveAt(model.Breadcrumbs.Count - 1);
            model.Breadcrumbs.Add(new Breadcrumb { Name = "My account", Url = new Uri("/account", UriKind.Relative) });

            var referrer = Request.GetTypedHeaders().Referer;
            if (referrer != null) { model.PreferredNextRoute = referrer.AbsolutePath; }

            return CurrentTemplate(model);
        }
    }
}