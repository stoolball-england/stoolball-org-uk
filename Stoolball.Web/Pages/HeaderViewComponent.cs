using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stoolball.Data.Abstractions;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Security;

namespace Stoolball.Web.Pages
{
    public class HeaderViewComponent : ViewComponent
    {
        private readonly IMemberManager _memberManager;
        private readonly IPlayerDataSource _playerDataSource;

        public HeaderViewComponent(IMemberManager memberManager, IPlayerDataSource playerDataSource)
        {
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
            _playerDataSource = playerDataSource ?? throw new ArgumentNullException(nameof(playerDataSource));
        }

        public async Task<IViewComponentResult> InvokeAsync(IHasViewMetadata model)
        {
            var headerModel = new HeaderViewModel(model);

            var currentMember = await _memberManager.GetCurrentMemberAsync();
            if (currentMember != null)
            {
                headerModel.Player = await _playerDataSource.ReadPlayerByMemberKey(currentMember.Key);
            }

            return View(headerModel);
        }
    }
}
