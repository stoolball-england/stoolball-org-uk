﻿using System.Collections.Generic;
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
        Club? CreateAuditableCopy(Club? club);
        Team? CreateAuditableCopy(Team? team);
        Team? CreateRedactedCopy(Team? team);
        TeamInMatch? CreateAuditableCopy(TeamInMatch? teamInMatch);
        Player? CreateAuditableCopy(Player? player);
        PlayerIdentity? CreateAuditableCopy(PlayerIdentity? playerIdentity);
        MatchLocation? CreateAuditableCopy(MatchLocation? matchLocation);
        MatchLocation? CreateRedactedCopy(MatchLocation? matchLocation);

        [return: NotNullIfNotNull(nameof(match))]
        Match? CreateAuditableCopy(Match? match);
        [return: NotNullIfNotNull(nameof(match))]
        Match? CreateRedactedCopy(Match? match);
        MatchInnings? CreateAuditableCopy(MatchInnings? innings);
        MatchAward? CreateAuditableCopy(MatchAward? award);
        Season? CreateAuditableCopy(Season? season);
        Season? CreateRedactedCopy(Season? season);
        Competition? CreateAuditableCopy(Competition? competition);
        Competition? CreateRedactedCopy(Competition? competition);
        Tournament? CreateAuditableCopy(Tournament? tournament);
        List<TeamInTournament> CreateAuditableCopy(List<TeamInTournament> teams);
        Tournament? CreateRedactedCopy(Tournament? tournament);
    }
}