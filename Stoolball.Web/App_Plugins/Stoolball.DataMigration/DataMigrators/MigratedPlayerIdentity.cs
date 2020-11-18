using System;
using System.Collections.Generic;
using Stoolball.Logging;
using Stoolball.Teams;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    public class MigratedPlayerIdentity : PlayerIdentity, IAuditable
    {
        public int MigratedPlayerIdentityId { get; set; }

        public int MigratedTeamId { get; set; }

        public string PlayerIdentityRoute { get; set; }

        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri {
            get { return new Uri($"https://www.stoolball.org.uk/id/player-identity/{PlayerIdentityId}"); }
        }
    }
}