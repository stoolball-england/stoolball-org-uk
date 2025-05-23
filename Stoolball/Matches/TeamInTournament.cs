﻿using System;
using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class TeamInTournament
    {
        public Guid? TournamentTeamId { get; set; }
        public Team? Team { get; set; }

        public TournamentTeamRole TeamRole { get; set; }
        public string? PlayingAsTeamName { get; set; }
    }
}
