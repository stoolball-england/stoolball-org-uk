using System;
using System.Collections.Generic;
using Stoolball.Clubs;
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
        internal Club? ClubWithMinimalDetails { get; set; }
        internal Club? ClubWithTeamsAndMatchLocation { get; set; }
        internal List<Club> Clubs { get; set; } = new();
        internal Player? BowlerWithMultipleIdentities { get; set; }
        internal List<Player> Players { get; set; } = new();
        internal List<PlayerInnings> PlayerInnings { get; set; } = new();
        internal List<PlayerIdentity> PlayerIdentities { get; set; } = new();
        internal List<Match> Matches { get; set; } = new();
        internal Team? TeamWithMinimalDetails { get; set; }
        internal Team? TeamWithFullDetails { get; set; }
        internal List<Team> Teams { get; set; } = new();
        internal List<TeamListing> TeamListings { get; set; } = new();
        internal MatchLocation? MatchLocationWithMinimalDetails { get; set; }
        internal MatchLocation? MatchLocationForClub { get; set; }
        internal List<MatchLocation> MatchLocations { get; set; } = new();
        internal List<Competition> Competitions { get; set; } = new();
        internal List<Player> PlayersWithMultipleIdentities { get; set; } = new();
        internal MatchLocation? MatchLocationWithFullDetails { get; set; }
        internal Competition? CompetitionWithFullDetails { get; set; }
        internal List<Season> Seasons { get; set; } = new();
        internal Season? SeasonWithFullDetails { get; set; }
        internal List<(Guid memberKey, string memberName)> Members { get; set; } = new();
        internal Match? MatchInThePastWithFullDetails { get; set; }
        internal Tournament? TournamentInThePastWithFullDetails { get; set; }
        internal List<Tournament> Tournaments { get; set; } = new();
        internal List<School> Schools { get; set; } = new();
        internal Tournament? TournamentInThePastWithMinimalDetails { get; set; }
        internal Tournament? TournamentInTheFutureWithMinimalDetails { get; set; }
    }
}
