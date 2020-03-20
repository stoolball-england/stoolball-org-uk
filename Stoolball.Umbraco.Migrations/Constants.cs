namespace Stoolball.Umbraco.Data
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
            public const string Queue = _tablePrefix + "Queue";

            public const string NotificationSubscription = _tablePrefix + "NotificationSubscription";
            public const string NotificationSent = _tablePrefix + "NotificationSent";

            public const string Batting = _tablePrefix + "Batting";
            public const string Bowling = _tablePrefix + "Bowling";
            public const string Club = _tablePrefix + "Club";
            public const string Competition = _tablePrefix + "Competition";
            public const string Ground = _tablePrefix + "Ground";
            public const string Match = _tablePrefix + "Match";
            public const string MatchComment = _tablePrefix + "MatchComment";
            public const string MatchTeam = _tablePrefix + "MatchTeam";
            public const string PlayerIdentity = _tablePrefix + "PlayerIdentity";
            public const string School = _tablePrefix + "School";
            public const string Season = _tablePrefix + "Season";
            public const string SeasonMatch = _tablePrefix + "SeasonMatch";
            public const string SeasonMatchType = _tablePrefix + "SeasonMatchType";
            public const string SeasonPoint = _tablePrefix + "SeasonPoint";
            public const string SeasonPointsRule = _tablePrefix + "SeasonPointsRule";
            public const string SeasonTeam = _tablePrefix + "SeasonTeam";
            public const string Team = _tablePrefix + "Team";

            public const string PlayerMatchStatistics = _tablePrefix + _statisticsPrefix + "PlayerMatch";
        }
    }
}
