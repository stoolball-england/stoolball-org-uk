using System.Threading.Tasks;
using Dapper;
using Stoolball.Statistics;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets statistics about team performances from the Umbraco database
    /// </summary>
    public class SqlServerInningsStatisticsDataSource : IInningsStatisticsDataSource, ICacheableInningsStatisticsDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IStatisticsQueryBuilder _statisticsQueryBuilder;

        public SqlServerInningsStatisticsDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IStatisticsQueryBuilder statisticsQueryBuilder)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new System.ArgumentNullException(nameof(databaseConnectionFactory));
            _statisticsQueryBuilder = statisticsQueryBuilder ?? throw new System.ArgumentNullException(nameof(statisticsQueryBuilder));
        }

        public async Task<InningsStatistics> ReadInningsStatistics(StatisticsFilter statisticsFilter)
        {
            if (statisticsFilter == null) { statisticsFilter = new StatisticsFilter(); }
            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(statisticsFilter);

            var sql = $@"SELECT AVG(CAST(TeamRunsScored AS DECIMAL)) AS AverageRunsScored, 
                                MAX(TeamRunsScored) AS HighestRunsScored, 
                                MIN(TeamRunsScored) AS LowestRunsScored,  

                                AVG(CAST(TeamRunsConceded AS DECIMAL)) AS AverageRunsConceded, 
                                MAX(TeamRunsConceded) AS HighestRunsConceded, 
                                MIN(TeamRunsConceded) AS LowestRunsConceded,  

                                AVG(CAST(TeamWicketsLost AS DECIMAL)) AS AverageWicketsLost,
                                AVG(CAST(TeamWicketsTaken AS DECIMAL)) AS AverageWicketsTaken
                        FROM (
                            SELECT SUM(DISTINCT TeamRunsScored) AS TeamRunsScored, 
                                   SUM(DISTINCT TeamWicketsLost) AS TeamWicketsLost, 
                                   SUM(DISTINCT TeamRunsConceded) AS TeamRunsConceded,
                                   SUM(DISTINCT TeamWicketsTaken) AS TeamWicketsTaken
                            FROM {Tables.PlayerInMatchStatistics} 
                            WHERE 1=1 {where}
                            GROUP BY MatchId, MatchTeamId, MatchInningsPair
                        ) AS MatchData";

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QuerySingleAsync<InningsStatistics>(sql, parameters).ConfigureAwait(false);
            }
        }
    }
}
