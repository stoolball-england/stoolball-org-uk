using Stoolball.Teams;
using System;
using System.Collections.Generic;

namespace Stoolball.Umbraco.Data.Teams
{
    public class PlayerIdentityQuery
    {
        public string Query { get; set; }
        public List<Guid> TeamIds { get; internal set; } = new List<Guid>();
        public List<PlayerRole> PlayerRoles { get; internal set; } = new List<PlayerRole>();
    }
}