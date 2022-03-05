using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Security
{
    public interface IMemberGroupHelper
    {
        SecurityGroup CreateOrFindGroup(string groupNamePrefix, string suggestedGroupName, IEnumerable<string> noiseWords);
        Task<bool> MemberIsAdministrator(string username);
        void AssignRole(string username, string roleName);
    }
}