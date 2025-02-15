﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Stoolball.Statistics;

namespace Stoolball.Matches
{
    public class MatchInnings
    {
        public Guid? MatchInningsId { get; set; }
        public int InningsOrderInMatch { get; set; }

        public int InningsPair()
        {
            var pair = InningsOrderInMatch;
            if (pair % 2 == 0) { pair--; };
            pair = pair - (int)Math.Floor((decimal)(pair / 2));
            return pair;
        }

        public Guid? BattingMatchTeamId { get; set; }
        public Guid? BowlingMatchTeamId { get; set; }
        public TeamInMatch? BattingTeam { get; set; }
        public TeamInMatch? BowlingTeam { get; set; }

        public List<OverSet> OverSets { get; internal set; } = new List<OverSet>();

        public List<PlayerInnings> PlayerInnings { get; internal set; } = new List<PlayerInnings>();
        public List<Over> OversBowled { get; internal set; } = new List<Over>();

        [Range(0, 1000000, ErrorMessage = "Byes must be a number, 0 or more")]
        public int? Byes { get; set; }

        [Range(0, 1000000, ErrorMessage = "Wides must be a number, 0 or more")]
        public int? Wides { get; set; }

        [Display(Name = "No balls")]
        [Range(0, 1000000, ErrorMessage = "No balls must be a number, 0 or more")]
        public int? NoBalls { get; set; }

        [Display(Name = "Bonus or penalty runs")]
        public int? BonusOrPenaltyRuns { get; set; }

        public bool HasExtras()
        {
            return Byes.HasValue || Wides.HasValue || NoBalls.HasValue || BonusOrPenaltyRuns.HasValue;
        }

        [Range(0, 1000000, ErrorMessage = "Runs must be a number, 0 or more")]
        public int? Runs { get; set; }

        [Range(0, 1000000, ErrorMessage = "Wickets must be a number, 0 or more")]
        public int? Wickets { get; set; }
        public Uri EntityUri {
            get { return new Uri($"https://www.stoolball.org.uk/id/match-innings/{MatchInningsId}"); }
        }

        public List<BowlingFigures> BowlingFigures { get; internal set; } = new List<BowlingFigures>();
    }
}
