﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Matches;
using Stoolball.Teams;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets stoolball match data from the Umbraco database
    /// </summary>
    public class SqlServerMatchListingDataSource : IMatchListingDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;

        public SqlServerMatchListingDataSource(IDatabaseConnectionFactory databaseConnectionFactory)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
        }

        /// <summary>
        /// Gets the number of matches and tournaments that match a query
        /// </summary>
        /// <returns></returns>
        public async Task<int> ReadTotalMatches(MatchQuery matchQuery)
        {
            if (matchQuery is null)
            {
                throw new ArgumentNullException(nameof(matchQuery));
            }

            if (!matchQuery.IncludeMatches && !matchQuery.IncludeTournaments)
            {
                return 0;
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var sql = new StringBuilder("SELECT SUM(Total) FROM (");
                var parameters = new Dictionary<string, object>();

                if (matchQuery.IncludeMatches)
                {
                    var (matchSql, matchParameters) = BuildMatchQuery(matchQuery,
                        $@"SELECT 1 AS GroupByThis, COUNT(DISTINCT m.MatchId) AS Total
                                FROM {Tables.Match} AS m
                                <<JOIN>>
                                <<WHERE>>");
                    sql.Append(matchSql);
                    parameters = matchParameters;
                }

                if (matchQuery.IncludeMatches && matchQuery.IncludeTournaments)
                {
                    sql.Append(" UNION ");
                }

                if (matchQuery.IncludeTournaments)
                {
                    var (tournamentSql, tournamentParameters) = BuildTournamentQuery(matchQuery,
                        $@"SELECT 1 AS GroupByThis, COUNT(DISTINCT tourney.TournamentId) AS Total
                                FROM {Tables.Tournament} AS tourney
                                <<JOIN>>
                                <<WHERE>>");
                    sql.Append(tournamentSql);
                    foreach (var key in tournamentParameters.Keys)
                    {
                        if (!parameters.ContainsKey(key))
                        {
                            parameters.Add(key, tournamentParameters[key]);
                        }
                    }
                }
                sql.Append(") AS x GROUP BY GroupByThis");

                return await connection.ExecuteScalarAsync<int>(sql.ToString(), new DynamicParameters(parameters)).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// Gets a list of matches and tournaments based on a query
        /// </summary>
        /// <returns>A list of <see cref="MatchListing"/> objects. An empty list if no matches or tournaments are found.</returns>
        public async Task<List<MatchListing>> ReadMatchListings(MatchQuery matchQuery)
        {
            if (matchQuery is null)
            {
                throw new ArgumentNullException(nameof(matchQuery));
            }

            if (!matchQuery.IncludeMatches && !matchQuery.IncludeTournaments)
            {
                return new List<MatchListing>();
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var sql = new StringBuilder();
                var parameters = new Dictionary<string, object>();
                var orderBy = new List<string>();

                if (matchQuery.IncludeMatches)
                {
                    var (matchSql, matchParameters) = BuildMatchQuery(matchQuery,
                        $@"SELECT m.MatchName, m.MatchRoute, m.StartTime, m.StartTimeIsKnown, m.MatchType, m.PlayerType, m.MatchResultType,
                                NULL AS TournamentQualificationType, NULL AS SpacesInTournament, m.OrderInTournament,
                                mt.TeamRole, mt.MatchTeamId,
                                mt.TeamId,
                                i.Runs, i.Wickets
                                FROM { Tables.Match } AS m
                                LEFT JOIN {Tables.MatchTeam} AS mt ON m.MatchId = mt.MatchId
                                LEFT JOIN {Tables.MatchInnings} AS i ON m.MatchId = i.MatchId AND i.BattingMatchTeamId = mt.MatchTeamId
                                <<JOIN>>
                                <<WHERE>> ");
                    sql.Append(matchSql);
                    parameters = matchParameters;

                    if (matchQuery.TournamentId != null)
                    {
                        orderBy.Add("OrderInTournament");
                    }
                }

                if (matchQuery.IncludeMatches && matchQuery.IncludeTournaments)
                {
                    sql.Append(" UNION ");
                }

                if (matchQuery.IncludeTournaments)
                {
                    var (tournamentSql, tournamentParameters) = BuildTournamentQuery(matchQuery,
                        $@"SELECT tourney.TournamentName AS MatchName, tourney.TournamentRoute AS MatchRoute, tourney.StartTime, tourney.StartTimeIsKnown, 
                                NULL AS MatchType, tourney.PlayerType, NULL AS MatchResultType,
                                tourney.QualificationType AS TournamentQualificationType, tourney.SpacesInTournament, NULL AS OrderInTournament,
                                NULL AS TeamRole, NULL AS MatchTeamId,
                                NULL AS TeamId,
                                NULL AS Runs, NULL AS Wickets
                                FROM { Tables.Tournament} AS tourney
                                <<JOIN>>
                                <<WHERE>> ");
                    sql.Append(tournamentSql);
                    foreach (var key in tournamentParameters.Keys)
                    {
                        if (!parameters.ContainsKey(key))
                        {
                            parameters.Add(key, tournamentParameters[key]);
                        }
                    }
                }

                orderBy.Add("StartTime");
                sql.Append("ORDER BY ").Append(string.Join(", ", orderBy.ToArray()));

                var matches = await connection.QueryAsync<MatchListing, TeamInMatch, Team, MatchInnings, MatchListing>(sql.ToString(),
                (matchListing, teamInMatch, team, matchInnings) =>
                {
                    if (teamInMatch != null)
                    {
                        teamInMatch.Team = team;
                        matchListing.Teams.Add(teamInMatch);

                        matchListing.MatchInnings.Add(new MatchInnings
                        {
                            BattingMatchTeamId = teamInMatch.MatchTeamId,
                            BattingTeam = teamInMatch,
                            Runs = matchInnings?.Runs,
                            Wickets = matchInnings?.Wickets
                        });
                    }
                    return matchListing;
                },
                new DynamicParameters(parameters),
                splitOn: "TeamRole, TeamId, Runs").ConfigureAwait(false);

                var listingsToReturn = matches.GroupBy(match => match.MatchRoute).Select(copiesOfMatch =>
                {
                    var matchToReturn = copiesOfMatch.First();
                    matchToReturn.MatchInnings = copiesOfMatch.Select(match => match.MatchInnings.SingleOrDefault()).OfType<MatchInnings>().ToList();
                    matchToReturn.Teams = copiesOfMatch.Select(match => match.Teams.SingleOrDefault()).OfType<TeamInMatch>().ToList();
                    return matchToReturn;
                }).ToList();

                return listingsToReturn;
            }
        }

        private static (string filteredSql, Dictionary<string, object> parameters) BuildMatchQuery(MatchQuery matchQuery, string sql)
        {
            if (matchQuery is null)
            {
                throw new ArgumentNullException(nameof(matchQuery));
            }

            var join = new List<string>();
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (matchQuery.MatchTypes?.Count > 0)
            {
                where.Add("m.MatchType IN @MatchTypes");
                parameters.Add("@MatchTypes", matchQuery.MatchTypes.Select(x => x.ToString()));
            }

            if (matchQuery.TeamIds?.Count > 0)
            {
                if (!sql.Contains(Tables.MatchTeam))
                {
                    join.Add($"INNER JOIN {Tables.MatchTeam} mt ON m.MatchId = mt.MatchId");
                }

                where.Add("mt.TeamId IN @TeamIds");
                parameters.Add("@TeamIds", matchQuery.TeamIds);
            }

            if (matchQuery.CompetitionIds?.Count > 0)
            {
                join.Add($"INNER JOIN {Tables.Season} s ON m.SeasonId = s.SeasonId");

                where.Add("s.CompetitionId IN @CompetitionIds");
                parameters.Add("@CompetitionIds", matchQuery.CompetitionIds);
            }

            if (matchQuery.SeasonIds?.Count > 0)
            {
                where.Add("m.SeasonId IN @SeasonIds");
                parameters.Add("@SeasonIds", matchQuery.SeasonIds);
            }

            if (matchQuery.MatchLocationIds?.Count > 0)
            {
                where.Add("m.MatchLocationId IN @MatchLocationIds");
                parameters.Add("@MatchLocationIds", matchQuery.MatchLocationIds);
            }

            if (matchQuery.FromDate != null)
            {
                where.Add("m.StartTime >= @FromDate");
                parameters.Add("@FromDate", matchQuery.FromDate.Value);
            }

            if (matchQuery.IncludeTournamentMatches == false)
            {
                where.Add("m.TournamentId IS NULL");
            }

            if (matchQuery.TournamentId != null)
            {
                where.Add("m.TournamentId = @TournamentId");
                parameters.Add("@TournamentId", matchQuery.TournamentId.Value);
            }

            sql = sql.Replace("<<JOIN>>", join.Count > 0 ? string.Join(" ", join) : string.Empty)
                     .Replace("<<WHERE>>", where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty);

            return (sql, parameters);
        }

        private static (string filteredSql, Dictionary<string, object> parameters) BuildTournamentQuery(MatchQuery matchQuery, string sql)
        {
            if (matchQuery is null)
            {
                throw new ArgumentNullException(nameof(matchQuery));
            }

            var join = new List<string>();
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (matchQuery.TeamIds?.Count > 0)
            {
                join.Add($"INNER JOIN {Tables.TournamentTeam} tt ON tourney.TournamentId = tt.TournamentId");

                where.Add("tt.TeamId IN @TeamIds");
                parameters.Add("@TeamIds", matchQuery.TeamIds);
            }

            if (matchQuery.CompetitionIds?.Count > 0)
            {
                join.Add($"INNER JOIN {Tables.TournamentSeason} ts ON tourney.TournamentId = ts.TournamentId");
                join.Add($"INNER JOIN {Tables.Season} s ON ts.SeasonId = s.SeasonId");

                where.Add("s.CompetitionId IN @CompetitionIds");
                parameters.Add("@CompetitionIds", matchQuery.CompetitionIds);
            }

            if (matchQuery.SeasonIds?.Count > 0)
            {
                if (!string.Join(string.Empty, join).Contains(Tables.TournamentSeason))
                {
                    join.Add($"INNER JOIN {Tables.TournamentSeason} ts ON tourney.TournamentId = ts.TournamentId");
                }

                where.Add("ts.SeasonId IN @SeasonIds");
                parameters.Add("@SeasonIds", matchQuery.SeasonIds);
            }

            if (matchQuery.MatchLocationIds?.Count > 0)
            {
                where.Add("tourney.MatchLocationId IN @MatchLocationIds");
                parameters.Add("@MatchLocationIds", matchQuery.MatchLocationIds);
            }

            if (matchQuery.FromDate != null)
            {
                where.Add("tourney.StartTime >= @FromDate");
                parameters.Add("@FromDate", matchQuery.FromDate.Value);
            }

            if (matchQuery.TournamentId != null)
            {
                where.Add("tourney.TournamentId = @TournamentId");
                parameters.Add("@TournamentId", matchQuery.TournamentId.Value);
            }

            sql = sql.Replace("<<JOIN>>", join.Count > 0 ? string.Join(" ", join) : string.Empty)
                     .Replace("<<WHERE>>", where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : string.Empty);

            return (sql, parameters);
        }
    }
}