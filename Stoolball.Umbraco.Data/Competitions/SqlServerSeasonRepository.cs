using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.Competitions;
using Stoolball.Umbraco.Data.Audit;
using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Competitions
{
    /// <summary>
    /// Writes stoolball season data to the Umbraco database
    /// </summary>
    public class SqlServerSeasonRepository : ISeasonRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IHtmlSanitizer _htmlSanitiser;

        public SqlServerSeasonRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger,
            IHtmlSanitizer htmlSanitiser)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _htmlSanitiser = htmlSanitiser ?? throw new ArgumentNullException(nameof(htmlSanitiser));

            _htmlSanitiser.AllowedTags.Clear();
            _htmlSanitiser.AllowedTags.Add("p");
            _htmlSanitiser.AllowedTags.Add("h2");
            _htmlSanitiser.AllowedTags.Add("strong");
            _htmlSanitiser.AllowedTags.Add("em");
            _htmlSanitiser.AllowedTags.Add("ul");
            _htmlSanitiser.AllowedTags.Add("ol");
            _htmlSanitiser.AllowedTags.Add("li");
            _htmlSanitiser.AllowedTags.Add("a");
            _htmlSanitiser.AllowedTags.Add("br");
            _htmlSanitiser.AllowedAttributes.Clear();
            _htmlSanitiser.AllowedAttributes.Add("href");
            _htmlSanitiser.AllowedCssProperties.Clear();
            _htmlSanitiser.AllowedAtRules.Clear();
        }

        /// <summary>
        /// Creates a stoolball season and populates the <see cref="Season.SeasonId"/>
        /// </summary>
        /// <returns>The created season</returns>
        public async Task<Season> CreateSeason(Season season, Guid memberKey, string memberName)
        {
            if (season is null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            try
            {
                season.SeasonId = Guid.NewGuid();
                season.Introduction = _htmlSanitiser.Sanitize(season.Introduction);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        season.SeasonRoute = $"{season.Competition.CompetitionRoute}/{season.StartYear}";
                        if (season.EndYear > season.StartYear)
                        {
                            season.SeasonRoute = $"{season.SeasonRoute}-{season.EndYear.ToString(CultureInfo.InvariantCulture).Substring(2)}";
                        }

                        await connection.ExecuteAsync(
                            $@"INSERT INTO {Tables.Season} (SeasonId, CompetitionId, StartYear, EndYear, Introduction, ShowTable, ShowRunsScored, ShowRunsConceded, SeasonRoute) 
                                VALUES (@SeasonId, @CompetitionId, @StartYear, @EndYear, @Introduction, @ShowTable, @ShowRunsScored, @ShowRunsConceded, @SeasonRoute)",
                            new
                            {
                                season.SeasonId,
                                season.Competition.CompetitionId,
                                season.StartYear,
                                season.EndYear,
                                season.Introduction,
                                season.ShowTable,
                                season.ShowRunsScored,
                                season.ShowRunsConceded,
                                season.SeasonRoute
                            }, transaction).ConfigureAwait(false);

                        foreach (var matchType in season.MatchTypes)
                        {
                            await connection.ExecuteAsync($@"INSERT INTO {Tables.SeasonMatchType} 
                                       (SeasonMatchTypeId, SeasonId, MatchType) 
                                        VALUES (@SeasonMatchTypeId, @SeasonId, @MatchType)",
                                        new
                                        {
                                            SeasonMatchTypeId = Guid.NewGuid(),
                                            season.SeasonId,
                                            MatchType = matchType.ToString()
                                        }, transaction).ConfigureAwait(false);
                        }

                        // Copy points rules from the most recent season
                        var pointsRules = await connection.QueryAsync<PointsRule>(
                            $@"SELECT MatchResultType, HomePoints, AwayPoints FROM { Tables.SeasonPointsRule } WHERE SeasonId = 
                                (
                                    SELECT TOP 1 SeasonId FROM StoolballSeason WHERE CompetitionId = @CompetitionId AND StartYear < @StartYear ORDER BY StartYear DESC
                                )",
                                new
                                {
                                    season.Competition.CompetitionId,
                                    season.StartYear
                                },
                                transaction).ConfigureAwait(false);

                        foreach (var pointsRule in pointsRules)
                        {
                            await connection.ExecuteAsync($@"INSERT INTO { Tables.SeasonPointsRule } 
                                (SeasonPointsRuleId, SeasonId, MatchResultType, HomePoints, AwayPoints)
                                VALUES (@SeasonPointsRuleId, @SeasonId, @MatchResultType, @HomePoints, @AwayPoints)",
                                new
                                {
                                    SeasonPointsRuleId = Guid.NewGuid(),
                                    season.SeasonId,
                                    pointsRule.MatchResultType,
                                    pointsRule.HomePoints,
                                    pointsRule.AwayPoints
                                },
                                transaction).ConfigureAwait(false);
                        }

                        // Copy teams fom the most recent season, where the teams did not withdraw and were still active in the season being added
                        var teamIds = await connection.QueryAsync<Guid>(
                            $@"SELECT t.TeamId FROM { Tables.SeasonTeam } st INNER JOIN { Tables.Team } t ON st.TeamId = t.TeamId 
                                WHERE st.SeasonId = (
                                    SELECT TOP 1 SeasonId FROM { Tables.Season } WHERE CompetitionId = @CompetitionId AND StartYear < @StartYear ORDER BY StartYear DESC
                                )
                                AND 
                                st.WithdrawnDate IS NULL
                                AND (t.UntilYear IS NULL OR t.UntilYear <= @StartYear)",
                            new
                            {
                                season.Competition.CompetitionId,
                                season.StartYear
                            },
                            transaction).ConfigureAwait(false);

                        foreach (var teamId in teamIds)
                        {
                            await connection.ExecuteAsync($@"INSERT INTO { Tables.SeasonTeam } 
                                (SeasonTeamId, SeasonId, TeamId)
                                VALUES (@SeasonTeamId, @SeasonId, @TeamId)",
                                new
                                {
                                    SeasonTeamId = Guid.NewGuid(),
                                    season.SeasonId,
                                    teamId
                                },
                                transaction).ConfigureAwait(false);
                        }

                        transaction.Commit();
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Create,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = season.EntityUri,
                    State = JsonConvert.SerializeObject(season),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerSeasonRepository), ex);
            }

            return season;
        }


        /// <summary>
        /// Updates a stoolball season
        /// </summary>
        public async Task<Season> UpdateSeason(Season season, Guid memberKey, string memberName)
        {
            if (season is null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            try
            {
                season.Introduction = _htmlSanitiser.Sanitize(season.Introduction);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {

                        await connection.ExecuteAsync(
                            $@"UPDATE {Tables.Season} SET
                                Introduction = @Introduction
						        WHERE SeasonId = @SeasonId",
                            new
                            {
                                season.Introduction,
                                season.SeasonId
                            }, transaction).ConfigureAwait(false);

                        await connection.ExecuteAsync($@"DELETE FROM {Tables.SeasonMatchType} WHERE SeasonId = @SeasonId AND MatchType NOT IN @MatchTypes",
                            new
                            {
                                season.SeasonId,
                                MatchTypes = season.MatchTypes.Select(x => x.ToString())
                            },
                            transaction).ConfigureAwait(false);

                        var currentMatchTypes = (await connection.QueryAsync<string>($"SELECT MatchType FROM {Tables.SeasonMatchType} WHERE SeasonId = @SeasonId", new { season.SeasonId }, transaction).ConfigureAwait(false)).ToList();
                        foreach (var matchType in season.MatchTypes)
                        {
                            if (!currentMatchTypes.Contains(matchType.ToString()))
                            {
                                await connection.ExecuteAsync($@"INSERT INTO {Tables.SeasonMatchType} 
                                       (SeasonMatchTypeId, SeasonId, MatchType) 
                                        VALUES (@SeasonMatchTypeId, @SeasonId, @MatchType)",
                                        new
                                        {
                                            SeasonMatchTypeId = Guid.NewGuid(),
                                            season.SeasonId,
                                            MatchType = matchType.ToString()
                                        }, transaction).ConfigureAwait(false);
                            }
                        }

                        transaction.Commit();
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Update,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = season.EntityUri,
                    State = JsonConvert.SerializeObject(season),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);

            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerSeasonRepository), ex);
            }

            return season;
        }
    }
}
