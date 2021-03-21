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
        public static class RegularExpressions
        {
            // From http://regexlib.com/REDetails.aspx?regexp_id=328
            public const string Email = @"((""[^""\f\n\r\t\v\b]+"")|([\w\!\#\$\%\&\'\*\+\-\~\/\^\`\|\{\}]+(\.[\w\!\#\$\%\&\'\*\+\-\~\/\^\`\|\{\}]+)*))@((\[(((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9])))\])|(((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9]))\.((25[0-5])|(2[0-4][0-9])|([0-1]?[0-9]?[0-9])))|((([A-Za-z0-9\-])+\.)+[A-Za-z\-]+))";
        }

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
            public const string Matches = "Matches";
            public const string MatchesUrl = "/matches";
            public const string MatchLocations = "Grounds and sports centres";
            public const string MatchLocationsUrl = "/locations";
            public const string Tournaments = "Tournaments";
            public const string TournamentsUrl = "/tournaments";
            public const string Statistics = "Statistics";
            public const string StatisticsUrl = "/play/statistics";
            public const string SeasonUrlRegEx = @"^[a-z0-9-]+\/[0-9]{4}(-[0-9]{2})?$";
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

        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Not a typical class. This is creating a set of constants accessible with IntelliSense.")]
        public static class EntityUriPrefixes
        {
            public const string Match = "https://www.stoolball.org.uk/id/match/";
            public const string Tournament = "https://www.stoolball.org.uk/id/tournament/";
        }
    }
}
