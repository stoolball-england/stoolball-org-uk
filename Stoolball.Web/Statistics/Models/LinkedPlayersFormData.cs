using System.Collections.Generic;
using Stoolball.Statistics;

namespace Stoolball.Web.Statistics.Models
{
    public class LinkedPlayersFormData
    {
        public List<PlayerIdentity> PlayerIdentities { get; set; } = new();
        public string? PreferredNextRoute { get; set; }
    }
}