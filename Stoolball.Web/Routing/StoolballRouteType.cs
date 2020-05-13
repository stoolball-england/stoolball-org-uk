namespace Stoolball.Web.Routing
{
    /// <summary>
    /// The types of route that can be handled by <see cref="StoolballRouteContentFinder"/> and <see cref="StoolballRouterController"/>
    /// </summary>
    public enum StoolballRouteType
    {
        Club,
        Clubs,
        EditClub,
        MatchLocation,
        MatchLocations,
        Team,
        TransientTeam,
        Teams,
        Season,
        Competition,
        MatchesForClub,
        MatchesForTeam,
        MatchesForMatchLocation,
        MatchesForSeason,
        Match,
        Tournament
    }
}