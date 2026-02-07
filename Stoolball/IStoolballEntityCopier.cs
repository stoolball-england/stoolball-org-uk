using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball
{
    public interface IStoolballEntityCopier
    {
        [return: NotNullIfNotNull(nameof(club))]
        Club? CreateAuditableCopy(Club? club);

        [return: NotNullIfNotNull(nameof(team))]
        Team? CreateAuditableCopy(Team? team);

        [return: NotNullIfNotNull(nameof(team))]
        Team? CreateRedactedCopy(Team? team);

        [return: NotNullIfNotNull(nameof(teamInMatch))]
        TeamInMatch? CreateAuditableCopy(TeamInMatch? teamInMatch);

        [return: NotNullIfNotNull(nameof(player))]
        Player? CreateAuditableCopy(Player? player);

        [return: NotNullIfNotNull(nameof(playerIdentity))]
        PlayerIdentity? CreateAuditableCopy(PlayerIdentity? playerIdentity);

        [return: NotNullIfNotNull(nameof(matchLocation))]
        MatchLocation? CreateAuditableCopy(MatchLocation? matchLocation);

        [return: NotNullIfNotNull(nameof(matchLocation))]
        MatchLocation? CreateRedactedCopy(MatchLocation? matchLocation);

        [return: NotNullIfNotNull(nameof(match))]
        Match? CreateAuditableCopy(Match? match);

        [return: NotNullIfNotNull(nameof(match))]
        Match? CreateRedactedCopy(Match? match);

        [return: NotNullIfNotNull(nameof(innings))]
        MatchInnings? CreateAuditableCopy(MatchInnings? innings);

        [return: NotNullIfNotNull(nameof(award))]
        MatchAward? CreateAuditableCopy(MatchAward? award);

        [return: NotNullIfNotNull(nameof(season))]
        Season? CreateAuditableCopy(Season? season);

        [return: NotNullIfNotNull(nameof(season))]
        Season? CreateRedactedCopy(Season? season);

        [return: NotNullIfNotNull(nameof(competition))]
        Competition? CreateAuditableCopy(Competition? competition);

        [return: NotNullIfNotNull(nameof(competition))]
        Competition? CreateRedactedCopy(Competition? competition);

        [return: NotNullIfNotNull(nameof(tournament))]
        Tournament? CreateAuditableCopy(Tournament? tournament);
        List<TeamInTournament> CreateAuditableCopy(List<TeamInTournament> teams);

        [return: NotNullIfNotNull(nameof(tournament))]
        Tournament? CreateRedactedCopy(Tournament? tournament);
    }
}