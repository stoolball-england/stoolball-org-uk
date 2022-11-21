using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Data.Abstractions;
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
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new System.ArgumentNullException(nameof(databaseConnectionFactory));
        }

        public async Task<InningsStatistics> ReadInningsStatistics(StatisticsFilter? statisticsFilter)
        {
            if (statisticsFilter == null) { statisticsFilter = new StatisticsFilter(); }

            var join = new List<string>();
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (statisticsFilter.FromDate.HasValue || statisticsFilter.UntilDate.HasValue)
            {
                join.Add($"INNER JOIN {Tables.Match} m ON mi.MatchId = m.MatchId");
            }

            if ((statisticsFilter.Club != null && statisticsFilter.Club.ClubId.HasValue) ||
                (statisticsFilter.Team != null && !string.IsNullOrEmpty(statisticsFilter.Team.TeamRoute)))
            {
                join.Add($"INNER JOIN {Tables.Team} t ON mt.TeamId = t.TeamId");
            }

            if (statisticsFilter.FromDate.HasValue)
            {
                where.Add("m.StartTime >= @FromDate");
                parameters.Add("@FromDate", statisticsFilter.FromDate);
            }

            if (statisticsFilter.UntilDate.HasValue)
            {
                where.Add("m.StartTime <= @UntilDate");
                parameters.Add("@UntilDate", statisticsFilter.UntilDate);
            }

            if (statisticsFilter.Club != null && statisticsFilter.Club.ClubId.HasValue)
            {
                where.Add("t.ClubId = @ClubId");
                parameters.Add("@ClubId", statisticsFilter.Club.ClubId);
            }

            if (statisticsFilter.Team != null && statisticsFilter.Team.TeamId.HasValue)
            {
                where.Add("mt.TeamId = @TeamId");
                parameters.Add("@TeamId", statisticsFilter.Team.TeamId);
            }

            if (statisticsFilter.Team != null && !string.IsNullOrEmpty(statisticsFilter.Team.TeamRoute))
            {
                where.Add("t.TeamRoute = @TeamRoute");
                parameters.Add("@TeamRoute", statisticsFilter.Team.TeamRoute);
            }

            var extraJoinsForFilters = string.Join(" ", join);
            var whereClause = where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty;

            var battingSql = $@"SELECT AVG(CAST(Runs AS DECIMAL)) AS AverageRunsScored, 
		                        MAX(Runs) AS HighestRunsScored,
		                        MIN(Runs) AS LowestRunsScored,
		                        AVG(CAST(Wickets AS DECIMAL)) AS AverageWicketsLost  
                         FROM {Tables.MatchInnings} mi 
                         INNER JOIN {Tables.MatchTeam} mt ON mi.BattingMatchTeamId = mt.MatchTeamId 
                         {extraJoinsForFilters}
                         {whereClause}";

            var bowlingSql = $@"SELECT AVG(CAST(Runs AS DECIMAL)) AS AverageRunsConceded, 
		                               MAX(Runs) AS HighestRunsConceded,
		                               MIN(Runs) AS LowestRunsConceded,
		                               AVG(CAST(Wickets AS DECIMAL)) AS AverageWicketsTaken  
                                FROM {Tables.MatchInnings} mi 
                                INNER JOIN {Tables.MatchTeam} mt ON mi.BowlingMatchTeamId = mt.MatchTeamId 
                                {extraJoinsForFilters}
                                {whereClause}";

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var battingStats = await connection.QuerySingleAsync<InningsStatistics>(battingSql, parameters).ConfigureAwait(false);
                var bowlingStats = await connection.QuerySingleAsync<InningsStatistics>(bowlingSql, parameters).ConfigureAwait(false);
                return new InningsStatistics
                {
                    AverageRunsScored = battingStats.AverageRunsScored,
                    AverageRunsConceded = bowlingStats.AverageRunsConceded,
                    AverageWicketsLost = battingStats.AverageWicketsLost,
                    AverageWicketsTaken = bowlingStats.AverageWicketsTaken,
                    HighestRunsScored = battingStats.HighestRunsScored,
                    LowestRunsScored = battingStats.LowestRunsScored,
                    HighestRunsConceded = bowlingStats.HighestRunsConceded,
                    LowestRunsConceded = bowlingStats.LowestRunsConceded
                };
            }
        }
    }
}
