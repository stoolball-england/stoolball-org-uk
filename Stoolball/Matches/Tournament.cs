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

        public string TournamentFullNameAndPlayerType(Func<DateTimeOffset, string> dateTimeFormatter)
        {
            if (dateTimeFormatter is null)
            {
                throw new ArgumentNullException(nameof(dateTimeFormatter));
            }

            var fullName = new StringBuilder(TournamentName);

            var saysTournament = TournamentName.ToUpperInvariant().Contains("TOURNAMENT");
            var playerType = PlayerType.Humanize(LetterCasing.Sentence);
            var saysPlayerType = TournamentName.Replace("'", string.Empty).ToUpperInvariant().Contains(playerType.ToUpperInvariant());

            if (!saysTournament && !saysPlayerType)
            {
                fullName.Append(" (").Append(playerType).Append(" tournament)");
            }
            else if (!saysTournament)
            {
                fullName.Append(" tournament");
            }
            else if (!saysPlayerType)
            {
                fullName.Append(" (").Append(playerType).Append(")");
            }

            fullName.Append(", ");
            fullName.Append(dateTimeFormatter(StartTime));

            return fullName.ToString();
        }

        public MatchLocation TournamentLocation { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public bool StartTimeIsKnown { get; set; }
        public PlayerType PlayerType { get; set; }
        public int? PlayersPerTeam { get; set; }
        public TournamentQualificationType? QualificationType { get; set; }
        public List<TeamInTournament> Teams { get; internal set; } = new List<TeamInTournament>();
        public int? OversPerInningsDefault { get; set; }
        public int? MaximumTeamsInTournament { get; set; }
        public int? SpacesInTournament { get; set; }
        public string TournamentNotes { get; set; }
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
            if (TournamentLocation != null) description.Append(" at ").Append(TournamentLocation.NameAndLocalityOrTown());

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

        /// <summary>
        /// Gets or sets the unique identifier of the member who owns the tournament
        /// </summary>
        public Guid? MemberKey { get; set; }
    }
}
