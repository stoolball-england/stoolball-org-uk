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
    public class Tournament : IAuditable
    {
        public Guid? TournamentId { get; set; }
        public string TournamentName { get; set; }
        public MatchLocation TournamentLocation { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public bool StartTimeIsKnown { get; set; }
        public PlayerType PlayerType { get; set; }
        public int? PlayersPerTeam { get; set; }
        public TournamentQualificationType? TournamentQualificationType { get; set; }
        public List<TeamInMatch> Teams { get; internal set; } = new List<TeamInMatch>();
        public int? OversPerInningsDefault { get; set; }
        public int? MaximumTeamsInTournament { get; set; }
        public int? SpacesInTournament { get; set; }
        public string MatchNotes { get; set; }
        public string TournamentRoute { get; set; }
        public List<Season> Seasons { get; internal set; } = new List<Season>();
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/tournament/{TournamentId}"); }
        }

        /// <summary>
        /// Gets a description of the tournament suitable for metadata or search results
        /// </summary>
        public string Description()
        {
            var description = new StringBuilder();

            description.Append("Stoolball tournament");
            if (TournamentLocation != null) description.Append(" at ").Append(TournamentLocation);

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

            return description.ToString();
        }
    }
}
