using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stoolball.Routing;
using Stoolball.Security;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using static Stoolball.Constants;

namespace Stoolball.Web.Security
{
    public class MemberGroupHelper : IMemberGroupHelper
    {
        private readonly IRouteGenerator _routeGenerator;
        private readonly IMemberService _memberService;
        private readonly IMemberGroupService _memberGroupService;
        private readonly IMemberManager _memberManager;

        public MemberGroupHelper(IRouteGenerator routeGenerator, IMemberService memberService, IMemberGroupService memberGroupService, IMemberManager memberManager)
        {
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
            _memberGroupService = memberGroupService ?? throw new ArgumentNullException(nameof(memberGroupService));
            _memberManager = memberManager ?? throw new ArgumentNullException(nameof(memberManager));
        }

        public SecurityGroup CreateOrFindGroup(string groupNamePrefix, string suggestedGroupName, IEnumerable<string> noiseWords)
        {
            var groupName = _routeGenerator.GenerateRoute(groupNamePrefix, suggestedGroupName, noiseWords);
            IMemberGroup group;
            do
            {
                group = _memberGroupService.GetByName(groupName);
                if (group == null)
                {
                    group = new MemberGroup
                    {
                        Name = groupName
                    };
                    _memberGroupService.Save(group);
                    break;
                }
                else
                {
                    groupName = _routeGenerator.IncrementRoute(groupName);
                }
            }
            while (group != null);

            return new SecurityGroup { Key = group!.Key, Name = group.Name };
        }

        public async Task<bool> MemberIsAdministrator(string username)
        {
            var member = await _memberManager.FindByNameAsync(username);
            if (string.IsNullOrEmpty(username)) { return false; }
            if (member == null) { return false; }
            return (await _memberManager.GetRolesAsync(member)).Any(x => x.ToUpperInvariant() == Groups.Administrators.ToUpperInvariant());
        }

        public void AssignRole(string username, string roleName)
        {
            _memberService.AssignRole(username, roleName);
        }
    }
}