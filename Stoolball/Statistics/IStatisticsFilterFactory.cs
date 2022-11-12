using System.Threading.Tasks;

namespace Stoolball.Statistics
{
    public interface IStatisticsFilterFactory
    {
        Task<StatisticsFilter> FromRoute(string route);
        Task<StatisticsFilter> FromQueryString(string? queryString);
    }
}