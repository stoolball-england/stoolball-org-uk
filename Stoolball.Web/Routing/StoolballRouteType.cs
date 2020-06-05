namespace Stoolball.Web.Routing
{
    /// <summary>
    /// The types of route that can be handled by <see cref="StoolballRouteContentFinder"/> and <see cref="StoolballRouterController"/>
    /// </summary>
    public enum StoolballRouteType
    {
        Club,
        ClubActions,
        Clubs,
        CreateClub,
        EditClub,
        DeleteClub,
        MatchLocation,
        MatchLocationActions,
        CreateMatchLocation,
        EditMatchLocation,
        DeleteMatchLocation,
        MatchLocations,
        Team,
        TransientTeam,
        TeamActions,
        CreateTeam,
        EditTeam,
        EditTransientTeam,
        DeleteTeam,
        Teams,
        Season,
        SeasonActions,
        CreateSeason,
        EditSeason,
        DeleteSeason,
        Competition,
        CompetitionActions,
        CreateCompetition,
        EditCompetition,
        DeleteCompetition,
        MatchesForClub,
        MatchesForTeam,
        MatchesForMatchLocation,
        MatchesForSeason,
        Match,
        Tournament,
    }
}