using System;

namespace Stoolball.Matches
{
    public interface IMatchInningsUrlParser
    {
        int? ParseInningsOrderInMatchFromUrl(Uri url);
    }
}