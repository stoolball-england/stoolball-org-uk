using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    ///  Constants for working with the stoolball data schema
    /// </summary>
    public static partial class Constants
    {
        internal const string _tablePrefix = "Stoolball";
        internal const string _statisticsPrefix = "Statistics";

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Not a typical class. This is creating a set of constants accessible with IntelliSense.")]
        public static class Tables
        {
            public const string Audit = _tablePrefix + "Audit";

            public const string PlayerInnings = _tablePrefix + "PlayerInnings";
            public const string Over = _tablePrefix + "Over";
            public const string Bowling = _tablePrefix + "Bowling";
            public const string FallOfWicket = _tablePrefix + "FallOfWicket";
            public const string Club = _tablePrefix + "Club";
            public const string ClubName = _tablePrefix + "ClubName";
            public const string Competition = _tablePrefix + "Competition";
            public const string Match = _tablePrefix + "Match";
            public const string MatchComment = _tablePrefix + "MatchComment";
            public const string MatchLocation = _tablePrefix + "MatchLocation";
            public const string MatchTeam = _tablePrefix + "MatchTeam";
            public const string MatchInnings = _tablePrefix + "MatchInnings";
            public const string MatchAward = _tablePrefix + "MatchAward";
            public const string Award = _tablePrefix + "Award";
            public const string Tournament = _tablePrefix + "Tournament";
            public const string TournamentTeam = _tablePrefix + "TournamentTeam";
            public const string TournamentSeason = _tablePrefix + "TournamentSeason";
            public const string TournamentComment = _tablePrefix + "TournamentComment";
            public const string Player = _tablePrefix + "Player";
            public const string PlayerIdentity = _tablePrefix + "PlayerIdentity";
            public const string School = _tablePrefix + "School";
            public const string SchoolName = _tablePrefix + "SchoolName";
            public const string Season = _tablePrefix + "Season";
            public const string SeasonMatchType = _tablePrefix + "SeasonMatchType";
            public const string SeasonPointsAdjustment = _tablePrefix + "SeasonPointsAdjustment";
            public const string SeasonPointsRule = _tablePrefix + "SeasonPointsRule";
            public const string SeasonTeam = _tablePrefix + "SeasonTeam";
            public const string Team = _tablePrefix + "Team";
            public const string TeamName = _tablePrefix + "TeamName";
            public const string TeamMatchLocation = _tablePrefix + "TeamMatchLocation";
            public const string NotificationSubscription = _tablePrefix + "NotificationSubscription";
            public const string StatisticsPlayerMatch = _tablePrefix + _statisticsPrefix + "PlayerMatch";

        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Not a typical class. This is creating a set of constants accessible with IntelliSense.")]
        public static class Groups
        {
            public const string Administrators = "Administrators";
            public const string AllMembers = "All Members";
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Not a typical class. This is creating a set of constants accessible with IntelliSense.")]
        public static class NoiseWords
        {
            public static IEnumerable<string> ClubRoute { get; } = new ReadOnlyCollection<string>(new[] { "stoolball", "club", "sports" });
            public static IEnumerable<string> SchoolRoute { get; } = new ReadOnlyCollection<string>(new[] { "stoolball" });
            public static IEnumerable<string> MatchLocationRoute { get; } = new ReadOnlyCollection<string>(new[] { "stoolball" });
            public static IEnumerable<string> TeamRoute { get; } = new ReadOnlyCollection<string>(new[] { "stoolball", "club" });
            public static IEnumerable<string> CompetitionRoute { get; } = Array.Empty<string>();
            public static IEnumerable<string> TournamentRoute { get; } = Array.Empty<string>();
            public static IEnumerable<string> MatchRoute { get; } = Array.Empty<string>();
            public static IEnumerable<string> PlayerRoute { get; } = Array.Empty<string>();
        }
    }
}
