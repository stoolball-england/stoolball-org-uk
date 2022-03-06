namespace Stoolball.Matches
{
    public interface IMatchesRssQueryStringParser
    {
        MatchFilter ParseFilterFromQueryString(string queryString);
    }
}