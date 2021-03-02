using System;
using System.Threading.Tasks;

namespace Stoolball.Statistics
{
    public interface IStatisticsFilterUrlParser
    {
        Task<StatisticsFilter> ParseUrl(Uri url);
    }
}