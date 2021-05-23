using System.Collections.Generic;
using Stoolball.Statistics;

namespace Stoolball.Data.SqlServer
{
    public interface IStatisticsQueryBuilder
    {
        (string where, Dictionary<string, object> parameters) BuildWhereClause(StatisticsFilter filter);
    }
}