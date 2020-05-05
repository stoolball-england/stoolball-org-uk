using Humanizer;
using Stoolball.Audit;
using Stoolball.Competitions;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stoolball.Matches
{
    public class Match : IAuditable
    {
        public Guid? MatchId { get; set; }
        public string MatchName { get; set; }
        public bool UpdateMatchNameAutomatically { get; set; }
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
        public List<Season> Seasons { get; internal set; } = new List<Season>();
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
            if (MatchType == MatchType.TournamentMatch)
            {
                if (Tournament != null)
                {
                    // Check for 'the' to get the grammar right
                    var tournamentName = Tournament.TournamentName.ToUpperInvariant();
                    var the = (tournamentName.Length >= 4 && tournamentName.Substring(0, 4) == "THE ") ? string.Empty : "the ";
                    description.Append("Match in ").Append(the).Append(Tournament.TournamentName);
                    if (MatchLocation != null) description.Append(" at ").Append(MatchLocation);
                    description.Append(".");
                }
            }
            else
            {
                description.Append("Stoolball ").Append(MatchType.Humanize(LetterCasing.LowerCase));
                if (MatchLocation != null) description.Append(" at ").Append(MatchLocation);

                var seasonList = string.Empty;

                if (Seasons.Count == 1)
                {
                    var season = Seasons.First();
                    var the = season.Competition.CompetitionName.ToUpperInvariant().Contains("THE ");
                    description.Append(" in ").Append(the ? string.Empty : "the ").Append(season.Competition.CompetitionName);
                }
                else if (Seasons.Count > 1)
                {
                    description.Append(" in ");
                    description.Append(Seasons.Humanize(x => x.Competition.CompetitionName));
                }

                description.Append(seasonList);

                description.Append('.');
            }

            return description.ToString();
        }
    }
}
