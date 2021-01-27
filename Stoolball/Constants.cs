using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Stoolball
{
    [ExcludeFromCodeCoverage]
    public static partial class Constants
    {
        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Not a typical class. This is creating a set of constants accessible with IntelliSense.")]
        public static class Pages
        {
            public const string Home = "Home";
            public const string HomeUrl = "/";
            public const string Players = "Play";
            public const string PlayersUrl = "/play";
            public const string Competitions = "Leagues and competitions";
            public const string CompetitionsUrl = "/competitions";
            public const string Teams = "Clubs and teams";
            public const string TeamsUrl = "/teams";
            public const string MatchLocations = "Grounds and sports centres";
            public const string MatchLocationsUrl = "/locations";
        }

        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Not a typical class. This is creating a set of constants accessible with IntelliSense.")]
        public static class Groups
        {
            public const string Administrators = "Administrators";
            public const string AllMembers = "All Members";
        }

        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Not a typical class. This is creating a set of constants accessible with IntelliSense.")]
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

        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Not a typical class. This is creating a set of constants accessible with IntelliSense.")]
        public static class LoggingTemplates
        {
            public const string Created = "Created {@Entity}. Created by {MemberName} {MemberKey} in {Type:l}.{Method:l}.";
            public const string Updated = "Updated {@Entity}. Updated by {MemberName} {MemberKey} in {Type:l}.{Method:l}.";
            public const string Deleted = "Deleted {@Entity}. Deleted by {MemberName} {MemberKey} in {Type:l}.{Method:l}.";
            public const string Migrated = "Migrated {@Entity} in {Type:l}.{Method:l}.";

            public const string CreateMember = "Created member {MemberUsername} {MemberKey} in {Type:l}.{Method:l}.";
            public const string ApproveMember = "Approved member {MemberUsername} {MemberKey} in {Type:l}.{Method:l}.";
            public const string MemberPasswordResetRequested = "Reset password requested for member {MemberUsername} {MemberKey} in {Type:l}.{Method:l}.";
            public const string MemberPasswordReset = "Reset password for member {MemberUsername} {MemberKey} in {Type:l}.{Method:l}.";
            public const string MemberAccountUpdated = "Account updated for member {MemberName} {MemberKey} in {Type:l}.{Method:l}.";
        }
        internal const string _tablePrefix = "Stoolball";
        internal const string _statisticsPrefix = "Statistics";

        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Not a typical class. This is creating a set of constants accessible with IntelliSense.")]
        public static class Tables
        {
            public const string Audit = _tablePrefix + "Audit";

            public const string PlayerInnings = _tablePrefix + "PlayerInnings";
            public const string Over = _tablePrefix + "Over";
            public const string BowlingFigures = _tablePrefix + "BowlingFigures";
            public const string FallOfWicket = _tablePrefix + "FallOfWicket";
            public const string Club = _tablePrefix + "Club";
            public const string ClubName = _tablePrefix + "ClubName";
            public const string Competition = _tablePrefix + "Competition";
            public const string Match = _tablePrefix + "Match";
            public const string MatchComment = _tablePrefix + "MatchComment";
            public const string MatchLocation = _tablePrefix + "MatchLocation";
            public const string MatchTeam = _tablePrefix + "MatchTeam";
            public const string MatchInnings = _tablePrefix + "MatchInnings";
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
            public const string Award = _tablePrefix + "Award";
            public const string AwardedTo = _tablePrefix + "AwardedTo";
            public const string AwardBy = _tablePrefix + "AwardBy";
            public const string NotificationSubscription = _tablePrefix + "NotificationSubscription";
            public const string StatisticsPlayerMatch = _tablePrefix + _statisticsPrefix + "PlayerMatch";
        }
    }
}
