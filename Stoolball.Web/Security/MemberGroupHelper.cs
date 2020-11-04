using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Security;
using Stoolball.Routing;
using Stoolball.Security;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using static Stoolball.Data.SqlServer.Constants;

namespace Stoolball.Web.Security
{
    public class MemberGroupHelper : IMemberGroupHelper
    {
        private readonly IRouteGenerator _routeGenerator;
        private readonly IMemberService _memberService;
        private readonly IMemberGroupService _memberGroupService;
        private readonly RoleProvider _roleProvider;

        public MemberGroupHelper(IRouteGenerator routeGenerator, IMemberService memberService, IMemberGroupService memberGroupService, RoleProvider roleProvider)
        {
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _memberService = memberService ?? throw new ArgumentNullException(nameof(memberService));
            _memberGroupService = memberGroupService ?? throw new ArgumentNullException(nameof(memberGroupService));
            _roleProvider = roleProvider ?? throw new ArgumentNullException(nameof(roleProvider));
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

            return new SecurityGroup { Key = group.Key, Name = group.Name };
        }

        public bool MemberIsAdministrator(string username)
        {
            return !string.IsNullOrEmpty(username) && _roleProvider.GetRolesForUser(username).Any(x => x.ToUpperInvariant() == Groups.Administrators.ToUpperInvariant());
        }

        public void AssignRole(string username, string roleName)
        {
            _memberService.AssignRole(username, roleName);
        }
    }
}