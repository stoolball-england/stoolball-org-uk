using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer
{
    public class SqlServerStatisticsDataSource : IStatisticsDataSource, ICacheableStatisticsDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

        public SqlServerStatisticsDataSource(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
        }

        public async virtual Task<int> ReadTotalPlayerInnings(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadTotalBestFiguresInAMatch("RunsScored", 0, filter).ConfigureAwait(false);
        }

        public async virtual Task<int> ReadTotalBowlingFigures(StatisticsFilter filter)
        {
            filter = filter ?? new StatisticsFilter();

            return await ReadTotalBestFiguresInAMatch("Wickets", 0, filter).ConfigureAwait(false);
        }

        private async Task<int> ReadTotalBestFiguresInAMatch(string primaryFieldName, int minimumValue, StatisticsFilter filter)
        {
            var (where, parameters) = BuildQuery(filter);
            where = $"WHERE {primaryFieldName} IS NOT NULL AND {primaryFieldName} >= {minimumValue} {where}";
            return await ReadTotalResultsForPagedQuery(where, parameters).ConfigureAwait(false);
        }

        private async Task<int> ReadTotalResultsForPagedQuery(string where, Dictionary<string, object> parameters, string groupBy = null, string having = null)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM (SELECT PlayerInMatchStatisticsId FROM {Tables.PlayerInMatchStatistics} {where} {groupBy} {having}) AS total", parameters).ConfigureAwait(false);
            }
        }

        public async virtual Task<IEnumerable<StatisticsResult<PlayerInnings>>> ReadPlayerInnings(StatisticsFilter filter, StatisticsSortOrder sortOrder)
        {
            filter = filter ?? new StatisticsFilter();

            var orderByFields = new List<string>();
            if (sortOrder == StatisticsSortOrder.LatestFirst)
            {
                orderByFields.Add("MatchStartTime DESC");
            }
            else if (sortOrder == StatisticsSortOrder.BestFirst)
            {
                orderByFields.Add("RunsScored DESC");
                orderByFields.Add("DismissalType ASC");
            }
            else
            {
                throw new InvalidOperationException();
            }

            return await ReadBestFiguresInAMatch<PlayerInnings>("RunsScored", "PlayerWasDismissed", new[] { "PlayerInningsId", "DismissalType", "BallsFaced" }, orderByFields, 0, filter).ConfigureAwait(false);
        }

        public async virtual Task<IEnumerable<StatisticsResult<BowlingFigures>>> ReadBowlingFigures(StatisticsFilter filter, StatisticsSortOrder sortOrder)
        {
            filter = filter ?? new StatisticsFilter();

            var orderByFields = new List<string>();
            if (sortOrder == StatisticsSortOrder.LatestFirst)
            {
                orderByFields.Add("MatchStartTime DESC");
            }
            else if (sortOrder == StatisticsSortOrder.BestFirst)
            {
                orderByFields.Add("Wickets DESC");
                orderByFields.Add("HasRunsConceded DESC");
                orderByFields.Add("RunsConceded ASC");
            }
            else
            {
                throw new InvalidOperationException();
            }

            // NOTE: Don't check for RunsConceded IS NOT NULL in this stat, because 5/NULL is still better than 4/20.
            // Wickets, even if 0, means the player bowled in the match, so check it is not null.
            // Can trust wickets, not other fields, because wickets figure is generated whether bowling is recorded on batting and/or bowling card.
            return await ReadBestFiguresInAMatch<BowlingFigures>("Wickets", "RunsConceded", new[] { "BowlingFiguresId", "RunsConceded", "Overs", "Maidens" }, orderByFields, 0, filter).ConfigureAwait(false);
        }


        /// <summary> 
        /// Gets best performances in a match based on the specified fields and filters
        /// </summary> 
        private async Task<IEnumerable<StatisticsResult<T>>> ReadBestFiguresInAMatch<T>(string primaryFieldName, string secondaryFieldNameForMaxResults, IEnumerable<string> selectFields, IEnumerable<string> orderByFields, int minimumValue, StatisticsFilter filter)
        {
            var select = $@"SELECT PlayerId, PlayerRoute, PlayerIdentityId, PlayerIdentityName, TeamId, TeamName,
                OppositionTeamId AS TeamId, OppositionTeamName AS TeamName, MatchRoute, MatchStartTime AS StartTime, MatchName, 
                {primaryFieldName}, {string.Join(", ", selectFields)}";

            var (where, parameters) = BuildQuery(filter);
            where = $"WHERE {primaryFieldName} IS NOT NULL AND {primaryFieldName} >= {minimumValue} {where}";

            var orderBy = orderByFields.Any() ? "ORDER BY " + string.Join(", ", orderByFields) : string.Empty;

            // The result set can be limited in two mutually-exlusive ways:
            // 1. Max results (eg top ten) but where results beyond but equal to the max are also included
            // 2. Paging
            var preQuery = string.Empty;
            var offsetWithExtraResults = string.Empty;
            var offsetPaging = string.Empty;
            if (filter.MaxResultsAllowingExtraResultsIfValuesAreEqual.HasValue)
            {
                // Get the values from what should be the last row according to the maximum number of results.
                preQuery = $@"DECLARE @MaxResult1 int, @MaxResult2 int;
                            SELECT @MaxResult1 = {primaryFieldName}, @MaxResult2 = {secondaryFieldNameForMaxResults} FROM {Tables.PlayerInMatchStatistics} {where} {orderBy}
                            OFFSET {filter.MaxResultsAllowingExtraResultsIfValuesAreEqual - 1} ROWS FETCH NEXT 1 ROWS ONLY; ";

                // If @MaxResult1 IS NULL there are fewer rows than the requested maximum, so just fetch all.
                // Otherwise look for results that are greater than or equal to the value(s) in the last row retrieved above.
                offsetWithExtraResults = $"AND (@MaxResult1 IS NULL OR ({primaryFieldName} > @MaxResult1 OR ({primaryFieldName} = @MaxResult1 AND ({secondaryFieldNameForMaxResults} <= @MaxResult2 OR @MaxResult2 IS NULL)))) ";
            }
            else
            {
                offsetPaging = $"OFFSET @PageOffset ROWS FETCH NEXT @PageSize ROWS ONLY";
                parameters.Add("@PageOffset", filter.Paging.PageSize * (filter.Paging.PageNumber - 1));
                parameters.Add("@PageSize", filter.Paging.PageSize);
            }

            var sql = $"{preQuery} {select} FROM {Tables.PlayerInMatchStatistics} {where} {offsetWithExtraResults} {orderBy} {offsetPaging}";

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                return await connection.QueryAsync<Player, PlayerIdentity, Team, Team, MatchListing, T, StatisticsResult<T>>(sql,
                    (player, playerIdentity, team, oppositionTeam, match, playerInnings) =>
                    {
                        player.PlayerIdentities.Add(playerIdentity);
                        return new StatisticsResult<T>
                        {
                            Player = player,
                            Team = team,
                            OppositionTeam = oppositionTeam,
                            Match = match,
                            Result = playerInnings
                        };
                    },
                    parameters,
                    splitOn: $"PlayerIdentityId, TeamId, TeamId, MatchRoute, {primaryFieldName}",
                    commandTimeout: 60).ConfigureAwait(false);
            }
        }

        /// <summary> 
        /// Adds standard filters to the WHERE clause
        /// </summary> 
        private static (string where, Dictionary<string, object> parameters) BuildQuery(StatisticsFilter filter)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (filter.PlayerOfTheMatch.HasValue)
            {
                where.Add("PlayerOfTheMatch = 1");
            }

            if (filter.Player != null)
            {
                where.Add("PlayerId = @PlayerId");
                parameters.Add("@PlayerId", filter.Player.PlayerId);
            }

            if (filter.BowledByPlayerIdentityIds.Any())
            {
                where.Add("BowledByPlayerIdentityId IN @BowlerPlayerIdentityIds");
                parameters.Add("@BowlerPlayerIdentityIds", filter.BowledByPlayerIdentityIds);
            }

            if (filter.CaughtByPlayerIdentityIds.Any())
            {
                where.Add("CaughtByPlayerIdentityId IN @CaughtByPlayerIdentityIds");
                parameters.Add("@CaughtByPlayerIdentityIds", filter.CaughtByPlayerIdentityIds);
            }

            if (filter.RunOutByPlayerIdentityIds.Any())
            {
                where.Add("RunOutByPlayerIdentityId IN @RunOutByPlayerIdentityIds");
                parameters.Add("@RunOutByPlayerIdentityIds", filter.RunOutByPlayerIdentityIds);
            }

            if (filter.Club != null)
            {
                where.Add("ClubId = @ClubId");
                parameters.Add("@ClubId", filter.Club.ClubId);
            }

            if (filter.SwapTeamAndOppositionFilters)
            {
                // When querying by the player id of a fielder, flip team && opposition because
                // the fielder's team is the opposition_id
                if (filter.Team != null)
                {
                    where.Add("OppositionTeamId = @OppositionTeamId");
                    parameters.Add("@OppositionTeamId", filter.Team.TeamId);
                }

                if (filter.OppositionTeamIds.Any())
                {
                    where.Add("TeamId = @TeamIds");
                    parameters.Add("@TeamIds", filter.OppositionTeamIds);
                }

                if (filter.WonMatch.HasValue)
                {
                    where.Add("WonMatch = @WonMatch");
                    parameters.Add("@WonMatch", filter.WonMatch.Value ? 0 : 1);
                }
            }
            else
            {
                if (filter.Team != null)
                {
                    where.Add("TeamId = @TeamId");
                    parameters.Add("@TeamId", filter.Team.TeamId);
                }

                if (filter.OppositionTeamIds.Any())
                {
                    where.Add("OppositionTeamId IN @OppositionTeamIds");
                    parameters.Add("@OppositionTeamIds", filter.OppositionTeamIds);
                }

                if (filter.WonMatch.HasValue)
                {
                    where.Add("WonMatch = @WonMatch");
                    parameters.Add("@WonMatch", filter.WonMatch.Value ? 1 : 0);
                }
            }

            if (filter.Season != null)
            {
                where.Add("SeasonId = @SeasonId");
                parameters.Add("@SeasonId", filter.Season.SeasonId);
            }

            if (filter.Competition != null)
            {
                where.Add("CompetitionId = @CompetitionId");
                parameters.Add("@CompetitionId", filter.Competition.CompetitionId);
            }

            if (filter.MatchLocation != null)
            {
                where.Add("MatchLocationId = @MatchLocationId");
                parameters.Add("@MatchLocationId", filter.MatchLocation.MatchLocationId);
            }

            if (filter.MatchTypes.Any())
            {
                where.Add("MatchType IN @MatchTypes");
                parameters.Add("@MatchTypes", filter.MatchTypes.Select(x => x.ToString()));
            }

            if (filter.PlayerTypes.Any())
            {
                where.Add("MatchPlayerType IN @MatchPlayerTypes");
                parameters.Add("@MatchPlayerTypes", filter.PlayerTypes.Select(x => x.ToString()));
            }

            if (filter.BattingPositions.Any())
            {
                where.Add("BattingPosition IN @BattingPositions");
                parameters.Add("@BattingPositions", filter.BattingPositions);
            }

            if (filter.TournamentIds.Any())
            {
                where.Add("TournamentId IN @TournamentIds");
                parameters.Add("@TournamentIds", filter.TournamentIds);
            }

            if (filter.DismissalTypes.Any())
            {
                where.Add("DismissalType IN @DismissalTypes");
                parameters.Add("@DismissalTypes", filter.DismissalTypes.Select(x => x.ToString()));
            }

            if (filter.FromDate.HasValue)
            {
                where.Add("MatchStartTime >= @FromDate");
                parameters.Add("@FromDate", filter.FromDate);
            }

            if (filter.UntilDate.HasValue)
            {
                where.Add("MatchStartTime <= @UntilDate");
                parameters.Add("@UntilDate", filter.UntilDate);
            }

            if (filter.BattingFirst.HasValue)
            {
                if (filter.SwapBattingFirstFilter)
                {
                    where.Add("BattedFirst = @BattedFirst");
                    parameters.Add("@BattedFirst", filter.BattingFirst.Value ? 0 : 1);
                }
                else
                {
                    where.Add("BattedFirst = @BattedFirst");
                    parameters.Add("@BattedFirst", filter.BattingFirst.Value ? 1 : 0);
                }
            }

            return (where.Count > 0 ? " AND " + string.Join(" AND ", where) : string.Empty, parameters);
        }
    }
}