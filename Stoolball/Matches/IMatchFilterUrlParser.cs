using System;

namespace Stoolball.Matches
{
    public interface IMatchFilterUrlParser
    {
        MatchFilter ParseUrl(Uri url);
    }
}