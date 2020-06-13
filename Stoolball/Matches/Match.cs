using Humanizer;
using Stoolball.Audit;
using Stoolball.Competitions;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stoolball.Matches
{
    public class Match : IAuditable
    {
        public Guid? MatchId { get; set; }
        public string MatchName { get; set; }
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

        public MatchLocation MatchLocation { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public bool StartTimeIsKnown { get; set; }
        public MatchType MatchType { get; set; }
        public PlayerType PlayerType { get; set; }
        public List<TeamInMatch> Teams { get; internal set; } = new List<TeamInMatch>();
        public int? PlayersPerTeam { get; set; }
        public Tournament Tournament { get; set; }
        public int? OversPerInningsDefault { get; set; }
        public int? OrderInTournament { get; set; }
        public bool InningsOrderIsKnown { get; set; }
        public MatchResultType? MatchResultType { get; set; }
        public List<MatchInnings> MatchInnings { get; internal set; } = new List<MatchInnings>();
        public string MatchNotes { get; set; }
        public string MatchRoute { get; set; }
        public Season Season { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();
        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/match/{MatchId}"); }
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
                description.Append(".");
            }
            else
            {
                description.Append("Stoolball ").Append(MatchType.Humanize(LetterCasing.LowerCase));
                if (MatchLocation != null) description.Append(" at ").Append(MatchLocation.NameAndLocalityOrTown());

                if (Season != null)
                {
                    var the = Season.Competition.CompetitionName.ToUpperInvariant().Contains("THE ");
                    description.Append(" in ").Append(the ? string.Empty : "the ").Append(Season.Competition.CompetitionName);
                }

                description.Append('.');
            }

            return description.ToString();
        }
    }
}
