using System.Collections.Generic;
using Stoolball.Security;

namespace Stoolball.Web.Security
{
    public class AuthorizedMembersViewModel
    {
        public string AuthorizedAction { get; set; } = "edit";
        public string? AuthorizationFor { get; set; }
        public Dictionary<AuthorizedAction, bool> CurrentMemberIsAuthorized { get; internal set; } = new();
        public List<string> AuthorizedGroupNames { get; set; } = new();

        public List<string> AuthorizedMemberNames { get; set; } = new();
        public string? Note { get; set; }
    }
}
