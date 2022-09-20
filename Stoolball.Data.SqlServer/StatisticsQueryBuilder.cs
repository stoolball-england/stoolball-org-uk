using System.Collections.Generic;
using System.Linq;
using Stoolball.Statistics;

namespace Stoolball.Data.SqlServer
{
    public class StatisticsQueryBuilder : IStatisticsQueryBuilder
    {
        /// <summary> 
        /// Adds standard filters to the WHERE clause
        /// </summary> 
        public (string where, Dictionary<string, object> parameters) BuildWhereClause(StatisticsFilter filter)
        {
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (filter.PlayerOfTheMatch.HasValue)
            {
                where.Add("PlayerOfTheMatch = 1");
            }

            if (filter.Player != null && filter.Player.PlayerId.HasValue)
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

            if (filter.Club != null && filter.Club.ClubId.HasValue)
            {
                where.Add("ClubId = @ClubId");
                parameters.Add("@ClubId", filter.Club.ClubId);
            }

            if (filter.SwapTeamAndOppositionFilters)
            {
                // When querying by the player id of a fielder, flip team && opposition because
                // the fielder's team is the opposition_id
                if (filter.Team != null && filter.Team.TeamId.HasValue)
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
                if (filter.Team != null && filter.Team.TeamId.HasValue)
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

            if (filter.Season != null && filter.Season.SeasonId.HasValue)
            {
                where.Add("SeasonId = @SeasonId");
                parameters.Add("@SeasonId", filter.Season.SeasonId);
            }

            if (filter.Competition != null && filter.Competition.CompetitionId.HasValue)
            {
                where.Add("CompetitionId = @CompetitionId");
                parameters.Add("@CompetitionId", filter.Competition.CompetitionId);
            }

            if (filter.MatchLocation != null && filter.MatchLocation.MatchLocationId.HasValue)
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
