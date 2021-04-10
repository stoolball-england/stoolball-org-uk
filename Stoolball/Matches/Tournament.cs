using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Humanizer;
using Stoolball.Competitions;
using Stoolball.Logging;
using Stoolball.MatchLocations;
using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class Tournament : IAuditable
    {
        public Guid? TournamentId { get; set; }

        [Display(Name = "Tournament name")]
        [Required]
        public string TournamentName { get; set; }

        public string TournamentFullName(Func<DateTimeOffset, string> dateTimeFormatter)
        {
            return TournamentFullNameAndPlayerType(dateTimeFormatter, false);
        }

        public string TournamentFullNameAndPlayerType(Func<DateTimeOffset, string> dateTimeFormatter)
        {
            return TournamentFullNameAndPlayerType(dateTimeFormatter, true);
        }

        private string TournamentFullNameAndPlayerType(Func<DateTimeOffset, string> dateTimeFormatter, bool includePlayerType)
        {
            if (dateTimeFormatter is null)
            {
                throw new ArgumentNullException(nameof(dateTimeFormatter));
            }

            var fullName = new StringBuilder(TournamentName);

            var saysTournament = TournamentName.ToUpperInvariant().Contains("TOURNAMENT");
            var playerType = PlayerType.Humanize(LetterCasing.Sentence);
            var saysPlayerType = TournamentName.Replace("'", string.Empty).ToUpperInvariant().Contains(playerType.ToUpperInvariant());

            if (includePlayerType && !saysTournament && !saysPlayerType)
            {
                fullName.Append(" (").Append(playerType).Append(" tournament)");
            }
            else if (!saysTournament)
            {
                fullName.Append(" tournament");
            }
            else if (includePlayerType && !saysPlayerType)
            {
                fullName.Append(" (").Append(playerType).Append(')');
            }

            fullName.Append(", ");
            fullName.Append(dateTimeFormatter(StartTime));

            return fullName.ToString();
        }

        public MatchLocation TournamentLocation { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public bool StartTimeIsKnown { get; set; }

        [Display(Name = "Who can play?")]
        [Required]
        public PlayerType PlayerType { get; set; }

        [Display(Name = "How many players per team?")]
        public int? PlayersPerTeam { get; set; }

        [Required]
        [Display(Name = "Which teams can enter?")]
        public TournamentQualificationType? QualificationType { get; set; }
        public List<TeamInTournament> Teams { get; internal set; } = new List<TeamInTournament>();

        public List<OverSet> DefaultOverSets { get; internal set; } = new List<OverSet>();

        [Display(Name = "How many teams do you have room for?")]
        [Range(3, 10000, ErrorMessage = "Tournaments must have at least 3 teams")] // Minimum 3, no real maximum
        public int? MaximumTeamsInTournament { get; set; }
        public int? SpacesInTournament { get; set; }

        [Display(Name = "Notes")]
        public string TournamentNotes { get; set; }
        public string TournamentRoute { get; set; }
        public List<Season> Seasons { get; internal set; } = new List<Season>();
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri {
            get { return new Uri($"{Constants.EntityUriPrefixes.Tournament}{TournamentId}"); }
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

        public MatchListing ToMatchListing()
        {
            return new MatchListing
            {
                MatchId = TournamentId.Value,
                MatchName = TournamentName,
                MatchRoute = TournamentRoute,
                StartTime = StartTime,
                StartTimeIsKnown = StartTimeIsKnown,
                PlayerType = PlayerType,
                PlayersPerTeam = PlayersPerTeam,
                TournamentQualificationType = QualificationType,
                SpacesInTournament = SpacesInTournament,
                MatchLocation = TournamentLocation
            };
        }
    }
}
