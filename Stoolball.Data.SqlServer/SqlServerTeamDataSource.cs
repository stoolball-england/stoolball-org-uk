using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Routing;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Gets stoolball team data from the Umbraco database
    /// </summary>
    public class SqlServerTeamDataSource : ITeamDataSource
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRouteNormaliser _routeNormaliser;

        public SqlServerTeamDataSource(IDatabaseConnectionFactory databaseConnectionFactory, IRouteNormaliser routeNormaliser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _routeNormaliser = routeNormaliser ?? throw new ArgumentNullException(nameof(routeNormaliser));
        }

        /// <summary>
        /// Gets the number of teams that match a query
        /// </summary>
        /// <returns></returns>
        public async Task<int> ReadTotalTeams(TeamFilter teamQuery)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var sql = $@"SELECT COUNT(DISTINCT t.TeamId)
                        FROM {Tables.Team} AS t
                        INNER JOIN {Tables.TeamVersion} AS tv ON t.TeamId = tv.TeamId
                        <<JOIN>>
                        <<WHERE>>
                        AND tv.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)";

                var (filteredSql, parameters) = ApplyTeamQuery(teamQuery, sql, Array.Empty<string>());

                return await connection.ExecuteScalarAsync<int>(filteredSql, new DynamicParameters(parameters)).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets a single team based on its route
        /// </summary>
        /// <param name="route">/teams/example-team</param>
        /// <param name="includeRelated"><c>true</c> to include the club, match locations and seasons; <c>false</c> otherwise</param>
        /// <returns>A matching <see cref="Team"/> or <c>null</c> if not found</returns>
        public async Task<Team> ReadTeamByRoute(string route, bool includeRelated = false)
        {
            return await (includeRelated ? ReadTeamWithRelatedDataByRoute(route) : ReadTeamByRoute(route)).ConfigureAwait(false);
        }

        private async Task<Team> ReadTeamByRoute(string route)
        {
            var normalisedRoute = NormaliseRouteToTeam(route);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var teams = await connection.QueryAsync<Team>(
                    $@"SELECT t.TeamId, tv.TeamName, t.TeamType, t.TeamRoute, YEAR(tv.UntilDate) AS UntilYear, t.MemberGroupName
                            FROM {Tables.Team} AS t 
                            INNER JOIN {Tables.TeamVersion} AS tv ON t.TeamId = tv.TeamId
                            WHERE LOWER(t.TeamRoute) = @Route
                            AND tv.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)",
                    new { Route = normalisedRoute }).ConfigureAwait(false);

                return teams.FirstOrDefault();
            }
        }

        private string NormaliseRouteToTeam(string route)
        {
            return _routeNormaliser.NormaliseRouteToEntity(route,
                                new Dictionary<string, string> {
                    { "teams", null },
                    {"tournaments", @"^[a-z0-9-]+\/teams\/[a-z0-9-]+$" }
                            });
        }

        private async Task<Team> ReadTeamWithRelatedDataByRoute(string route)
        {
            var normalisedRoute = NormaliseRouteToTeam(route);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                // The selected match location fields ensure that match locations are sorted the same way as in SqlServerTeamListingDataSource, meaning the team is assigned the same home location by Team.TeamNameLocationAndPlayerType()
                var teams = await connection.QueryAsync<Team, Club, MatchLocation, Season, Competition, string, Team>(
                    $@"SELECT t.TeamId, tv.TeamName, t.TeamType, t.PlayerType, t.Introduction, t.AgeRangeLower, t.AgeRangeUpper, t.ClubMark,
                            t.Facebook, t.Twitter, t.Instagram, t.YouTube, t.Website, t.PublicContactDetails, t.PrivateContactDetails, 
                            t.PlayingTimes, t.Cost, t.TeamRoute, YEAR(tv.UntilDate) AS UntilYear, t.MemberGroupKey, t.MemberGroupName,
                            cn.ClubName, c.ClubRoute, 
                            ml.MatchLocationId, ml.SecondaryAddressableObjectName, ml.PrimaryAddressableObjectName, ml.Locality, 
                            ml.Town, ml.AdministrativeArea, ml.MatchLocationRoute,
                            s.SeasonId, s.FromYear, s.UntilYear, s.SeasonRoute,
                            co.CompetitionId, cv.CompetitionName,
                            mt.MatchType
                            FROM {Tables.Team} AS t 
                            INNER JOIN {Tables.TeamVersion} AS tv ON t.TeamId = tv.TeamId 
                            LEFT JOIN {Tables.Club} AS c ON t.ClubId = c.ClubId
                            LEFT JOIN {Tables.ClubVersion} AS cn ON c.ClubId = cn.ClubId AND cn.UntilDate IS NULL
                            LEFT JOIN {Tables.TeamMatchLocation} AS tml ON tml.TeamId = t.TeamId AND tml.UntilDate IS NULL 
                            LEFT JOIN {Tables.MatchLocation} AS ml ON ml.MatchLocationId = tml.MatchLocationId 
                            LEFT JOIN {Tables.SeasonTeam} AS st ON t.TeamId = st.TeamId
                            LEFT JOIN {Tables.Season} AS s ON st.SeasonId = s.SeasonId
                            LEFT JOIN {Tables.Competition} AS co ON co.CompetitionId = s.CompetitionId
                            LEFT JOIN {Tables.CompetitionVersion} AS cv ON co.CompetitionId = cv.CompetitionId
                            LEFT JOIN {Tables.SeasonMatchType} AS mt ON s.SeasonId = mt.SeasonId
                            WHERE LOWER(t.TeamRoute) = @Route
                            AND tv.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                            AND (cv.CompetitionVersionId = (SELECT TOP 1 CompetitionVersionId FROM {Tables.CompetitionVersion} WHERE CompetitionId = co.CompetitionId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC) OR cv.CompetitionVersionId IS NULL)
                            ORDER BY cv.ComparableName, s.FromYear DESC, s.UntilYear ASC",
                    (team, club, matchLocation, season, competition, matchType) =>
                    {
                        team.Club = club;
                        if (matchLocation != null)
                        {
                            team.MatchLocations.Add(matchLocation);
                        }
                        if (season != null)
                        {
                            season.Competition = competition;
                            if (!string.IsNullOrEmpty(matchType))
                            {
                                season.MatchTypes.Add((MatchType)Enum.Parse(typeof(MatchType), matchType));
                            }
                            team.Seasons.Add(new TeamInSeason { Season = season });
                        }
                        return team;
                    },
                    new { Route = normalisedRoute },
                    splitOn: "ClubName, MatchLocationId, SeasonId, CompetitionId, MatchType").ConfigureAwait(false);

                var teamToReturn = teams.FirstOrDefault(); // get an example with the properties that are the same for every row
                if (teamToReturn != null)
                {
                    teamToReturn.MatchLocations = teams.Select(team => team.MatchLocations.SingleOrDefault())
                        .OfType<MatchLocation>()
                        .Distinct(new MatchLocationEqualityComparer())
                        .OrderBy(x => x.ComparableName())
                        .ToList();
                    teamToReturn.Seasons = teams.Select(team => team.Seasons.SingleOrDefault())
                        .OfType<TeamInSeason>()
                        .Distinct(new TeamInSeasonEqualityComparer())
                        .ToList();

                    var allSeasons = teams.SelectMany(team => team.Seasons);
                    foreach (var teamInSeason in teamToReturn.Seasons)
                    {
                        teamInSeason.Season.MatchTypes = allSeasons
                            .Where(season => season.Season.SeasonId == teamInSeason.Season.SeasonId)
                            .Select(season => season.Season.MatchTypes.FirstOrDefault())
                            .OfType<MatchType>()
                            .Distinct()
                            .ToList();
                    }
                }

                return teamToReturn;
            }
        }

        /// <summary>
        /// Gets a list of teams based on a query
        /// </summary>
        /// <returns>A list of <see cref="Team"/> objects. An empty list if no teams are found.</returns>
        public async Task<List<Team>> ReadTeams(TeamFilter teamQuery)
        {
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                var sql = $@"SELECT t.TeamId, tv.TeamName, t.TeamRoute, t.PlayerType, YEAR(tv.UntilDate) AS UntilYear,
                            ml.MatchLocationId, ml.Locality, ml.Town
                            FROM {Tables.Team} AS t 
                            INNER JOIN {Tables.TeamVersion} AS tv ON t.TeamId = tv.TeamId
                            LEFT JOIN {Tables.TeamMatchLocation} AS tml ON tml.TeamId = t.TeamId AND tml.UntilDate IS NULL
                            LEFT JOIN {Tables.MatchLocation} AS ml ON ml.MatchLocationId = tml.MatchLocationId 
                            <<JOIN>>
                            <<WHERE>>
                            AND tv.TeamVersionId = (SELECT TOP 1 TeamVersionId FROM {Tables.TeamVersion} WHERE TeamId = t.TeamId ORDER BY ISNULL(UntilDate, '{SqlDateTime.MaxValue.Value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}') DESC)
                            ORDER BY CASE WHEN tv.UntilDate IS NULL THEN 0 
                                          WHEN tv.UntilDate IS NOT NULL THEN 1 END, tv.TeamName";

                var (filteredSql, parameters) = ApplyTeamQuery(teamQuery, sql, new[] { Tables.TeamMatchLocation, Tables.MatchLocation });

                var teams = await connection.QueryAsync<Team, MatchLocation, Team>(filteredSql,
                    (team, matchLocation) =>
                    {
                        if (matchLocation != null)
                        {
                            team.MatchLocations.Add(matchLocation);
                        }
                        return team;
                    },
                    new DynamicParameters(parameters),
                    splitOn: "MatchLocationId").ConfigureAwait(false);

                var resolvedTeams = teams.GroupBy(team => team.TeamId).Select(copiesOfTeam =>
                {
                    var resolvedTeam = copiesOfTeam.First();
                    resolvedTeam.MatchLocations = copiesOfTeam
                        .Select(team => team.MatchLocations.SingleOrDefault())
                        .OfType<MatchLocation>()
                        .Distinct(new MatchLocationEqualityComparer())
                        .ToList();
                    return resolvedTeam;
                }).ToList();

                return resolvedTeams;
            }
        }

        private static (string filteredSql, Dictionary<string, object> parameters) ApplyTeamQuery(TeamFilter teamQuery, string sql, string[] currentJoinsBeyondTeamVersion)
        {
            var join = new List<string>();
            var where = new List<string>();
            var parameters = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(teamQuery?.Query))
            {
                if (!currentJoinsBeyondTeamVersion.Contains(Tables.TeamMatchLocation))
                {
                    join.Add($"LEFT JOIN {Tables.TeamMatchLocation} AS tml ON tml.TeamId = t.TeamId AND tml.UntilDate IS NULL");
                }
                if (!currentJoinsBeyondTeamVersion.Contains(Tables.MatchLocation))
                {
                    join.Add($"LEFT JOIN {Tables.MatchLocation} AS ml ON ml.MatchLocationId = tml.MatchLocationId ");
                }
                where.Add("(tv.TeamName LIKE @Query OR ml.Locality LIKE @Query OR ml.Town LIKE @Query OR ml.AdministrativeArea LIKE @Query)");
                parameters.Add("@Query", $"%{teamQuery.Query}%");
            }

            if (teamQuery?.CompetitionIds?.Count > 0)
            {
                if (!currentJoinsBeyondTeamVersion.Contains(Tables.SeasonTeam))
                {
                    join.Add($"INNER JOIN {Tables.SeasonTeam} st ON t.TeamId = st.TeamId");
                }
                if (!currentJoinsBeyondTeamVersion.Contains(Tables.Season))
                {
                    join.Add($"INNER JOIN {Tables.Season} s ON st.SeasonId = s.SeasonId");
                }

                where.Add("s.CompetitionId IN @CompetitionIds");
                parameters.Add("@CompetitionIds", teamQuery.CompetitionIds);
            }

            if (teamQuery?.ExcludeTeamIds?.Count > 0)
            {
                where.Add("t.TeamId NOT IN @ExcludeTeamIds");
                parameters.Add("@ExcludeTeamIds", teamQuery.ExcludeTeamIds.Select(x => x.ToString()));
            }

            if (teamQuery?.TeamTypes?.Count > 0)
            {
                where.Add("t.TeamType IN @TeamTypes");
                parameters.Add("@TeamTypes", teamQuery.TeamTypes.Select(x => x.ToString()));
            }

            if (teamQuery != null && !teamQuery.IncludeClubTeams)
            {
                where.Add("t.ClubId IS NULL");
            }

            sql = sql.Replace("<<JOIN>>", join.Count > 0 ? string.Join(" ", join) : string.Empty)
                     .Replace("<<WHERE>>", where.Count > 0 ? "WHERE " + string.Join(" AND ", where) : "WHERE 1=1"); // there must be a where clause because some callers append to it

            return (sql, parameters);
        }
    }
}
