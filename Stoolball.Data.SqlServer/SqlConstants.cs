namespace Stoolball.Data.SqlServer
{
    public static class Tables
    {

        internal const string _tablePrefix = "Stoolball";
        internal const string _statisticsPrefix = "Statistics";

        public const string Audit = _tablePrefix + "Audit";
        public const string PlayerInnings = _tablePrefix + "PlayerInnings";
        public const string Over = _tablePrefix + "Over";
        public const string OverSet = _tablePrefix + "OverSet";
        public const string BowlingFigures = _tablePrefix + "BowlingFigures";
        public const string FallOfWicket = _tablePrefix + "FallOfWicket";
        public const string Club = _tablePrefix + "Club";
        public const string ClubVersion = _tablePrefix + "ClubVersion";
        public const string Competition = _tablePrefix + "Competition";
        public const string CompetitionVersion = _tablePrefix + "CompetitionVersion";
        public const string Match = _tablePrefix + "Match";
        public const string MatchLocation = _tablePrefix + "MatchLocation";
        public const string MatchTeam = _tablePrefix + "MatchTeam";
        public const string MatchInnings = _tablePrefix + "MatchInnings";
        public const string Tournament = _tablePrefix + "Tournament";
        public const string TournamentTeam = _tablePrefix + "TournamentTeam";
        public const string TournamentSeason = _tablePrefix + "TournamentSeason";
        public const string Comment = _tablePrefix + "Comment";
        public const string Player = _tablePrefix + "Player";
        public const string PlayerIdentity = _tablePrefix + "PlayerIdentity";
        public const string School = _tablePrefix + "School";
        public const string SchoolVersion = _tablePrefix + "SchoolVersion";
        public const string Season = _tablePrefix + "Season";
        public const string SeasonMatchType = _tablePrefix + "SeasonMatchType";
        public const string PointsAdjustment = _tablePrefix + "PointsAdjustment";
        public const string PointsRule = _tablePrefix + "PointsRule";
        public const string SeasonTeam = _tablePrefix + "SeasonTeam";
        public const string Team = _tablePrefix + "Team";
        public const string TeamVersion = _tablePrefix + "TeamVersion";
        public const string TeamMatchLocation = _tablePrefix + "TeamMatchLocation";
        public const string Award = _tablePrefix + "Award";
        public const string AwardedTo = _tablePrefix + "AwardedTo";
        public const string NotificationSubscription = _tablePrefix + "NotificationSubscription";
        public const string StatisticsPlayerMatch = _tablePrefix + _statisticsPrefix + "PlayerMatch";
    }
}
