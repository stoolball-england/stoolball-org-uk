using Stoolball.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Security;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Security
{
    public class MemberGroupHelper : IMemberGroupHelper
    {
        private readonly IRouteGenerator _routeGenerator;
        private readonly IMemberGroupService _memberGroupService;
        private readonly RoleProvider _roleProvider;

        public MemberGroupHelper(IRouteGenerator routeGenerator, IMemberGroupService memberGroupService, RoleProvider roleProvider)
        {
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _memberGroupService = memberGroupService ?? throw new ArgumentNullException(nameof(memberGroupService));
            _roleProvider = roleProvider ?? throw new ArgumentNullException(nameof(roleProvider));
        }

        public IMemberGroup CreateOrFindGroup(string groupNamePrefix, string suggestedGroupName, IEnumerable<string> noiseWords)
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

            return group;
        }

        public bool MemberIsAdministrator(string username)
        {
            return !string.IsNullOrEmpty(username) && _roleProvider.GetRolesForUser(username).Any(x => x.ToUpperInvariant() == Groups.Administrators.ToUpperInvariant());
        }
    }
}