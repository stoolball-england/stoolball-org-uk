﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

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
            public const string Matches = "Matches and tournaments";
            public const string MatchesUrl = "/matches";
            public const string MatchLocations = "Grounds and sports centres";
            public const string MatchLocationsUrl = "/locations";
            public const string Tournaments = "Matches and tournaments";
            public const string TournamentsUrl = "/tournaments";
            public const string Statistics = "Statistics";
            public const string StatisticsUrl = "/play/statistics";
            public const string SeasonUrlRegEx = @"^[a-z0-9-]+\/[0-9]{4}(-[0-9]{2})?$";
            public const string Schools = "Schools";
            public const string SchoolsUrl = "/schools";
            public const string AccountUrl = "/account";
            public const string SignInUrl = "/account/sign-in";
            public const string SignOutUrl = "/account/sign-out";
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

            public const string CreateMember = "Created member {MemberUsername} {MemberKey}. Email sent in {Type:l}.{Method:l}.";
            public const string ApproveMember = "Approved member {MemberUsername} {MemberKey} in {Type:l}.{Method:l}.";
            public const string MemberAlreadyExists = "Cannot create member for {Email} because it is already in use. Email sent in {Type:l}.{Method:l}.";
            public const string MemberPasswordResetRequested = "Reset password requested for member {MemberUsername} {MemberKey}. Email sent in {Type:l}.{Method:l}.";
            public const string PasswordResetForNonMemberRequested = "Reset password requested for non-member {Email}. Email sent in {Type:l}.{Method:l}.";
            public const string MemberPasswordReset = "Reset password for member {MemberUsername} {MemberKey} in {Type:l}.{Method:l}.";
            public const string MemberPasswordResetTokenInvalid = "Password reset token invalid {Token} in {Type:l}.{Method:l}.";
            public const string MemberPersonalDetailsUpdated = "Account updated for member {MemberName} {MemberKey} in {Type:l}.{Method:l}.";
            public const string MemberRequestedEmailAddressAlreadyInUse = "Email address edit to address already in use, requested for member {MemberName} {MemberKey}. Email sent in {Type:l}.{Method:l}.";
            public const string MemberRequestedEmailAddress = "Email address edit requested for member {MemberName} {MemberKey}. Email sent in {Type:l}.{Method:l}.";
            public const string ConfirmEmailAddress = "Confirmed email address for member {MemberUsername} {MemberKey} in {Type:l}.{Method:l}.";
            public const string MemberNotActivatedOrLockedOut = "Member {MemberKey} is not activated or locked out. Email sent in {Type:l}.{Method:l}.";
        }

        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Not a typical class. This is creating a set of constants accessible with IntelliSense.")]
        public static class EntityUriPrefixes
        {
            public const string Match = "https://www.stoolball.org.uk/id/match/";
            public const string Tournament = "https://www.stoolball.org.uk/id/tournament/";
        }

        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Not a typical class. This is creating a set of constants accessible with IntelliSense.")]
        public static class Defaults
        {
            public const int PageSize = 50;
        }

        [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Not a typical class. This is creating a set of constants accessible with IntelliSense.")]
        public static class QueryParameters
        {
            public const string ConfirmPlayerLinkedToMember = "player-added";
        }

        public static string UkTimeZone() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "GMT Standard Time" : "Europe/London";
    }
}
