using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Matches;
using Stoolball.Statistics;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets players' performances from the Umbraco database
    /// </summary>
    public class SqlServerPlayerPerformanceStatisticsDataSource : IPlayerPerformanceStatisticsDataSource, ICacheablePlayerPerformanceStatisticsDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IStatisticsQueryBuilder _statisticsQueryBuilder;

        public SqlServerPlayerPerformanceStatisticsDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IStatisticsQueryBuilder statisticsQueryBuilder)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _statisticsQueryBuilder = statisticsQueryBuilder ?? throw new ArgumentNullException(nameof(statisticsQueryBuilder));
        }

        public async Task<IEnumerable<StatisticsResult<PlayerInnings>>> ReadPlayerInnings(StatisticsFilter filter)
        {
            if (filter == null) { throw new ArgumentNullException(nameof(filter)); }

            var sql = $@"SELECT MatchName, MatchRoute, MatchStartTime AS StartTime, PlayerIdentityName, PlayerRoute, PlayerInningsId, DismissalType, RunsScored, BowledByPlayerIdentityName AS PlayerIdentityName, BowledByPlayerRoute AS PlayerRoute
                         FROM {Tables.PlayerInMatchStatistics}
                         <<WHERE>>
                         ORDER BY MatchStartTime DESC
                         OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY";

            var (where, parameters) = _statisticsQueryBuilder.BuildWhereClause(filter);
            sql = sql.Replace("<<WHERE>>", $"WHERE PlayerInningsId IS NOT NULL {where}");

            parameters.Add("PageOffset", (filter.Paging.PageNumber - 1) * filter.Paging.PageSize);
            parameters.Add("PageSize", filter.Paging.PageSize);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QueryAsync<MatchListing, PlayerIdentity, Player, PlayerInnings, PlayerIdentity, Player, StatisticsResult<PlayerInnings>>(sql,
                    (match, batterIdentity, batter, playerInnings, bowlerIdentity, bowler) =>
                    {
                        batterIdentity.Player = batter;
                        playerInnings.Batter = batterIdentity;
                        if (bowlerIdentity != null)
                        {
                            bowlerIdentity.Player = bowler;
                            playerInnings.Bowler = bowlerIdentity;
                        }
                        return new StatisticsResult<PlayerInnings>
                        {
                            Match = match,
                            Result = playerInnings
                        };
                    },
                    parameters,
                    splitOn: "PlayerIdentityName, PlayerRoute, PlayerInningsId, PlayerIdentityName, PlayerRoute").ConfigureAwait(false);
            }
        }
    }
}
