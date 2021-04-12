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
        public SqlServerInningsStatisticsDataSource(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _databaseConnectionFactory = databaseConnectionFactory;
        }

        public async Task<InningsStatistics> ReadInningsStatistics(StatisticsFilter statisticsFilter)
        {
            if (statisticsFilter == null) { statisticsFilter = new StatisticsFilter(); }

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
                            <<WHERE>>
                            GROUP BY MatchId, MatchTeamId, MatchInningsPair
                        ) AS MatchData";

            var where = string.Empty;
            if (statisticsFilter.Club != null)
            {
                where = "WHERE ClubId = @ClubId ";
            }
            else if (statisticsFilter.Team != null)
            {
                where = "WHERE TeamId = @TeamId ";
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QuerySingleAsync<InningsStatistics>(sql.Replace("<<WHERE>>", where), new
                {
                    statisticsFilter.Club?.ClubId,
                    statisticsFilter.Team?.TeamId
                })
                .ConfigureAwait(false);
            }
        }
    }
}
