﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Humanizer;
using Stoolball.Comments;
using Stoolball.Competitions;
using Stoolball.Logging;
using Stoolball.MatchLocations;
using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class Match : IAuditable
    {
        public Guid? MatchId { get; set; }

        public string? MatchName { get; set; }
        public bool UpdateMatchNameAutomatically { get; set; }

        public string MatchFullName(Func<DateTimeOffset, string> dateTimeFormatter)
        {
            if (dateTimeFormatter is null)
            {
                throw new ArgumentNullException(nameof(dateTimeFormatter));
            }

            var fullName = new StringBuilder(MatchName);

            if (Tournament != null)
            {
                fullName.Append(Tournament.TournamentName.StartsWith("THE ", StringComparison.OrdinalIgnoreCase) ? " in " : " in the ");
                fullName.Append(Tournament.TournamentName);
            }
            fullName.Append(", ");
            fullName.Append(dateTimeFormatter(StartTime));

            return fullName.ToString();
        }

        public MatchLocation? MatchLocation { get; set; }

        [Display(Name = "Match date")]
        public DateTimeOffset StartTime { get; set; }
        public bool StartTimeIsKnown { get; set; }
        public MatchType MatchType { get; set; }
        public PlayerType PlayerType { get; set; }
        public List<TeamInMatch> Teams { get; internal set; } = new List<TeamInMatch>();
        public int? PlayersPerTeam { get; set; }
        public bool EnableBonusOrPenaltyRuns { get; set; }
        public bool LastPlayerBatsOn { get; set; }
        public Tournament? Tournament { get; set; }
        public int? OrderInTournament { get; set; }
        public bool InningsOrderIsKnown { get; set; }
        public MatchResultType? MatchResultType { get; set; }
        public List<MatchInnings> MatchInnings { get; internal set; } = new List<MatchInnings>();

        public bool HasScorecard()
        {
            var hasBatting = MatchInnings.Select(x => x.PlayerInnings.Count).Sum() > 0;
            var hasOvers = MatchInnings.Select(x => x.OversBowled.Count).Sum() > 0;
            var hasRuns = MatchInnings.Any(x => x.Runs.HasValue);
            var hasWickets = MatchInnings.Any(x => x.Wickets.HasValue);
            var hasExtras = MatchInnings.Any(x => x.HasExtras());

            return (hasBatting || hasOvers || hasRuns || hasWickets || hasExtras);
        }

        public bool HasCompleteScorecard()
        {
            if (MatchResultType.HasValue && new List<Matches.MatchResultType> { Matches.MatchResultType.HomeWin, Matches.MatchResultType.AwayWin, Matches.MatchResultType.Tie }.Contains(MatchResultType.Value))
            {
                var hasMissingBatting = MatchInnings.Any(x => x.PlayerInnings.Count == 0);
                var hasMissingOvers = MatchInnings.Any(x => x.OversBowled.Count == 0);
                var hasMissingRuns = MatchInnings.Select(x => x.Runs.HasValue).Contains(false);
                var hasMissingWickets = MatchInnings.Select(x => x.Wickets.HasValue).Contains(false);

                return !(hasMissingBatting || hasMissingOvers || hasMissingRuns || hasMissingWickets);
            }
            else return true; // no need for a complete scorecard unless it's a home or away win or tie
        }

        public List<MatchAward> Awards { get; internal set; } = new List<MatchAward>();

        [Display(Name = "Notes")]
        public string? MatchNotes { get; set; }
        public string? MatchRoute { get; set; }
        public Season? Season { get; set; }
        public List<HtmlComment> Comments { get; internal set; } = new List<HtmlComment>();
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();
        public Uri EntityUri {
            get { return new Uri($"{Constants.EntityUriPrefixes.Match}{MatchId}"); }
        }

        /// <summary>
        /// Gets a description of the match suitable for metadata or search results
        /// </summary>
        public string Description()
        {
            var description = new StringBuilder();

            // Display match type/season/tournament
            if (Tournament != null)
            {
                // Check for 'the' to get the grammar right
                var the = (Tournament.TournamentName.StartsWith("THE ", StringComparison.OrdinalIgnoreCase)) ? string.Empty : "the ";
                description.Append("Match in ").Append(the).Append(Tournament.TournamentName);
                if (MatchLocation != null) description.Append(" at ").Append(MatchLocation.NameAndLocalityOrTown());
                description.Append('.');
            }
            else
            {
                description.Append("Stoolball ").Append(MatchType.Humanize(LetterCasing.LowerCase));
                if (MatchLocation != null) description.Append(" at ").Append(MatchLocation.NameAndLocalityOrTown());

                if (Season != null)
                {
                    var the = Season.Competition?.CompetitionName?.ToUpperInvariant().Contains("THE ") ?? false;
                    description.Append(" in ").Append(the ? string.Empty : "the ").Append(Season.Competition?.CompetitionName);
                }

                description.Append('.');
            }

            return description.ToString();
        }

        /// <summary>
        /// Gets or sets the unique identifier of the member who owns the match
        /// </summary>
        public Guid? MemberKey { get; set; }

        /// <summary>
        /// Gets the unique identifiers of the members who own the match and, if applicable, its containing tournament
        /// </summary>
        public IEnumerable<Guid> MemberKeys()
        {
            var keys = new List<Guid>();
            if (MemberKey.HasValue)
            {
                keys.Add(MemberKey.Value);
            }
            if (Tournament != null && Tournament.MemberKey.HasValue && !keys.Contains(Tournament.MemberKey.Value))
            {
                keys.Add(Tournament.MemberKey.Value);
            }
            return keys;
        }

        /// <summary>
        ///  Gets the member group names who can edit specifically this match (ie: not including groups who can edit any match) 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> MemberGroupNames()
        {
            var groups = new List<string>();

            foreach (var team in Teams)
            {
                if (!string.IsNullOrEmpty(team.Team.MemberGroupName) && !groups.Contains(team.Team.MemberGroupName))
                {
                    groups.Add(team.Team.MemberGroupName);
                }
            }

            if (Season != null && Season.Competition != null && !string.IsNullOrEmpty(Season.Competition.MemberGroupName))
            {
                groups.Add(Season.Competition.MemberGroupName);
            }

            return groups;
        }
        public MatchListing ToMatchListing()
        {
            return new MatchListing
            {
                MatchId = MatchId,
                MatchInnings = MatchInnings,
                MatchName = MatchName,
                MatchRoute = MatchRoute,
                MatchResultType = MatchResultType,
                MatchType = MatchType,
                PlayerType = PlayerType,
                PlayersPerTeam = PlayersPerTeam,
                StartTime = StartTime,
                StartTimeIsKnown = StartTimeIsKnown,
                Teams = Teams,
                MatchLocation = MatchLocation
            };
        }
    }
}
