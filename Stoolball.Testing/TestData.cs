using System;
using System.Collections.Generic;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Schools;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Testing
{
    public class TestData
    {
        internal Player? BowlerWithMultipleIdentities { get; set; }
        internal List<Player> Players { get; set; } = new List<Player>();
        internal List<PlayerInnings> PlayerInnings { get; set; } = new List<PlayerInnings>();
        internal List<PlayerIdentity> PlayerIdentities { get; set; } = new List<PlayerIdentity>();
        internal List<Match> Matches { get; set; } = new List<Match>();
        internal Team? TeamWithFullDetails { get; set; }
        internal List<Team> Teams { get; set; } = new List<Team>();
        internal List<MatchLocation> MatchLocations { get; set; } = new List<MatchLocation>();
        internal List<Competition> Competitions { get; set; } = new List<Competition>();
        internal List<Player> PlayersWithMultipleIdentities { get; set; } = new List<Player>();
        internal MatchLocation? MatchLocationWithFullDetails { get; set; }
        internal Competition? CompetitionWithFullDetails { get; set; }
        internal List<Season> Seasons { get; set; } = new List<Season>();
        internal Season? SeasonWithFullDetails { get; set; }
        internal List<(Guid memberKey, string memberName)> Members { get; set; } = new();
        internal Match? MatchInThePastWithFullDetails { get; set; }
        internal Tournament? TournamentInThePastWithFullDetails { get; set; }
        internal List<Tournament> Tournaments { get; set; } = new List<Tournament>();
        internal List<School> Schools { get; set; } = new List<School>();
        internal Tournament? TournamentInThePastWithMinimalDetails { get; set; }
        internal Tournament? TournamentInTheFutureWithMinimalDetails { get; set; }
    }
}
