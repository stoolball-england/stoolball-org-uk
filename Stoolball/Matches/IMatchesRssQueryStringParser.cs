using System.Collections.Specialized;

namespace Stoolball.Matches
{
    public interface IMatchesRssQueryStringParser
    {
        MatchFilter ParseFilterFromQueryString(NameValueCollection queryString);
    }
}