using System.Collections.Generic;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    public class TestData
    {
        public Player BowlerWithMultipleIdentities { get; internal set; }
        public List<Player> Players { get; internal set; } = new List<Player>();
        public List<PlayerInnings> PlayerInnings { get; internal set; } = new List<PlayerInnings>();
        public List<PlayerIdentity> PlayerIdentities { get; internal set; } = new List<PlayerIdentity>();
        public List<Match> Matches { get; internal set; } = new List<Match>();
        public Team TeamWithClub { get; internal set; }
        public List<Team> Teams { get; internal set; } = new List<Team>();
        public List<MatchLocation> MatchLocations { get; internal set; } = new List<MatchLocation>();
        public List<Competition> Competitions { get; internal set; } = new List<Competition>();
        public List<Player> PlayersWithMultipleIdentities { get; internal set; } = new List<Player>();
    }
}
