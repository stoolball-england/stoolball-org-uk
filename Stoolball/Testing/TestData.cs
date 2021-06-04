using System;
using System.Collections.Generic;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Testing
{
    public class TestData
    {
        public Player BowlerWithMultipleIdentities { get; internal set; }
        public List<Player> Players { get; internal set; } = new List<Player>();
        public List<PlayerInnings> PlayerInnings { get; internal set; } = new List<PlayerInnings>();
        public List<PlayerIdentity> PlayerIdentities { get; internal set; } = new List<PlayerIdentity>();
        public List<Match> Matches { get; internal set; } = new List<Match>();
        public Team TeamWithFullDetails { get; internal set; }
        public List<Team> Teams { get; internal set; } = new List<Team>();
        public List<MatchLocation> MatchLocations { get; internal set; } = new List<MatchLocation>();
        public List<Competition> Competitions { get; internal set; } = new List<Competition>();
        public List<Player> PlayersWithMultipleIdentities { get; internal set; } = new List<Player>();
        public MatchLocation MatchLocationWithFullDetails { get; internal set; }
        public Competition CompetitionWithFullDetails { get; internal set; }
        public List<Season> Seasons { get; internal set; } = new List<Season>();
        public Season SeasonWithFullDetails { get; internal set; }
        public List<(Guid memberId, string)> Members { get; internal set; }
        public Match MatchInThePastWithFullDetails { get; internal set; }
        public Tournament TournamentWithFullDetails { get; internal set; }
        public List<Tournament> Tournaments { get; internal set; } = new List<Tournament>();
    }
}
