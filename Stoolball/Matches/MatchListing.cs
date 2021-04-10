using System;
using System.Collections.Generic;
using System.Text;
using Humanizer;
using Stoolball.MatchLocations;
using Stoolball.Teams;

namespace Stoolball.Matches
{
    public class MatchListing
    {
        public Guid MatchId { get; set; }
        public string MatchName { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public bool StartTimeIsKnown { get; set; }

        /// <summary>
        /// Gets an estimated end time for the match or tournament based on the start time and a typical duration
        /// </summary>
        /// <returns></returns>
        public DateTimeOffset EstimatedEndTime()
        {
            if (StartTimeIsKnown)
            {
                return (MatchType == null) ? StartTime.AddHours(7) : StartTime.AddMinutes(90);
            }
            else
            {
                return StartTime.AddDays(1);
            }
        }

        public MatchType? MatchType { get; set; }
        public PlayerType PlayerType { get; set; }
        public int? PlayersPerTeam { get; set; }
        public List<TeamInMatch> Teams { get; internal set; } = new List<TeamInMatch>();
        public MatchResultType? MatchResultType { get; set; }
        public List<MatchInnings> MatchInnings { get; internal set; } = new List<MatchInnings>();

        /// <summary>
        ///  Gets whether the match was won by the home team
        /// </summary>
        public bool IsHomeWin()
        {
            if (MatchResultType == Matches.MatchResultType.HomeWin) return true;
            if (MatchResultType == Matches.MatchResultType.HomeWinByForfeit) return true;
            return false;
        }

        /// <summary>
        ///  Gets whether the match was won by the away team
        /// </summary>
        public bool IsAwayWin()
        {
            if (MatchResultType == Matches.MatchResultType.AwayWin) return true;
            if (MatchResultType == Matches.MatchResultType.AwayWinByForfeit) return true;
            return false;
        }

        /// <summary>
        ///  Gets whether the match was won or tied
        /// </summary>
        public bool IsEqualResult()
        {
            if (MatchResultType == Matches.MatchResultType.Tie) return true;
            return false;
        }

        /// <summary>
        ///  Gets whether the match was cancelled or abandoned
        /// </summary>
        public bool IsNoResult()
        {
            if (MatchResultType == Matches.MatchResultType.AbandonedDuringPlayAndCancelled) return true;
            if (MatchResultType == Matches.MatchResultType.AbandonedDuringPlayAndPostponed) return true;
            if (MatchResultType == Matches.MatchResultType.Cancelled) return true;
            if (MatchResultType == Matches.MatchResultType.Postponed) return true;
            return false;
        }

        public TournamentQualificationType? TournamentQualificationType { get; set; }
        public int? SpacesInTournament { get; set; }
        public string MatchRoute { get; set; }
        public MatchLocation MatchLocation { get; set; }

        public DateTimeOffset? FirstAuditDate { get; set; }
        public DateTimeOffset? LastAuditDate { get; set; }

        /// <summary>
        /// Gets a description of the match suitable for metadata or search results
        /// </summary>
        public string Description()
        {
            var description = new StringBuilder(PlayerType.Humanize(LetterCasing.Sentence)).Append(" stoolball ");

            description.Append(MatchType == null ? "tournament" : MatchType.Humanize(LetterCasing.LowerCase));
            if (MatchLocation != null)
            {
                description.Append(" at ").Append(MatchLocation.NameAndLocalityOrTown());
            }
            description.Append(". ");

            if (TournamentQualificationType == Matches.TournamentQualificationType.OpenTournament)
            {
                description.Append("Any team may enter this tournament. ");
            }
            else if (TournamentQualificationType == Matches.TournamentQualificationType.ClosedTournament)
            {
                description.Append("This tournament is for invited or qualifying teams only. ");
            }

            if (PlayersPerTeam.HasValue)
            {
                description.Append(PlayersPerTeam).Append(" players per team. ");
            }

            return description.ToString().TrimEnd();
        }
    }
}
