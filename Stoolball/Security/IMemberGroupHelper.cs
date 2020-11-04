using System.Collections.Generic;

namespace Stoolball.Security
{
    public interface IMemberGroupHelper
    {
        SecurityGroup CreateOrFindGroup(string groupNamePrefix, string suggestedGroupName, IEnumerable<string> noiseWords);
        bool MemberIsAdministrator(string username);
        void AssignRole(string username, string roleName);
    }
}