using System.Collections.Generic;
using Umbraco.Core.Models;

namespace Stoolball.Umbraco.Data.Security
{
    public interface IMemberGroupHelper
    {
        IMemberGroup CreateOrFindGroup(string groupNamePrefix, string suggestedGroupName, IEnumerable<string> noiseWords);
        bool MemberIsAdministrator(string username);
    }
}