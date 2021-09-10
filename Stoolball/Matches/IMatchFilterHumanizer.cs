namespace Stoolball.Matches
{
    public interface IMatchFilterHumanizer
    {
        string MatchesAndTournaments(MatchFilter filter);
        string MatchesAndTournamentsMatchingFilter(MatchFilter filter);
        string MatchingFilter(MatchFilter filter);
    }
}