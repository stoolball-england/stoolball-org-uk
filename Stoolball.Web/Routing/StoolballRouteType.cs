namespace Stoolball.Web.Routing
{
    /// <summary>
    /// The types of route that can be handled by <see cref="StoolballRouteContentFinder"/> and <see cref="StoolballRouterController"/>
    /// </summary>
    public enum StoolballRouteType
    {
        Club,
        Clubs,
        CreateClub,
        EditClub,
        MatchLocation,
        MatchLocations,
        CreateMatchLocation,
        EditMatchLocation,
        Team,
        TransientTeam,
        CreateTeam,
        EditTeam,
        EditTransientTeam,
        Teams,
        Season,
        Competition,
        CreateCompetition,
        EditCompetition,
        MatchesForClub,
        MatchesForTeam,
        MatchesForMatchLocation,
        MatchesForSeason,
        Match,
        Tournament,
    }
}