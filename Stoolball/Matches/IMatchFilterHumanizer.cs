namespace Stoolball.Matches
{
    public interface IMatchFilterHumanizer
    {
        string EntitiesMatchingFilter(string entities, string matchingFilter);
        string MatchesAndTournaments(MatchFilter filter);
        string MatchingFilter(MatchFilter filter);
    }
}