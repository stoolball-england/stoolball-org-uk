using System;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
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
        private readonly IRedirectsRepository _redirectsRepository;

        public SqlServerSeasonRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger,
            IHtmlSanitizer htmlSanitiser, IRedirectsRepository redirectsRepository)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _htmlSanitiser = htmlSanitiser ?? throw new ArgumentNullException(nameof(htmlSanitiser));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
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
                season.Results = _htmlSanitiser.Sanitize(season.Results);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        season.SeasonRoute = $"{season.Competition.CompetitionRoute}/{season.FromYear}";
                        if (season.UntilYear > season.FromYear)
                        {
                            season.SeasonRoute = $"{season.SeasonRoute}-{season.UntilYear.ToString(CultureInfo.InvariantCulture).Substring(2)}";
                        }

                        await connection.ExecuteAsync(
                            $@"INSERT INTO {Tables.Season} (SeasonId, CompetitionId, FromYear, UntilYear, Introduction, EnableTournaments, 
                                PlayersPerTeam, Overs, EnableLastPlayerBatsOn, ResultsTableType, EnableRunsScored, EnableRunsConceded, Results, SeasonRoute) 
                                VALUES 
                                (@SeasonId, @CompetitionId, @FromYear, @UntilYear, @Introduction, @EnableTournaments, @PlayersPerTeam, @Overs,
                                 @EnableLastPlayerBatsOn, @ResultsTableType, @EnableRunsScored, @EnableRunsConceded, @Results, @SeasonRoute)",
                            new
                            {
                                season.SeasonId,
                                season.Competition.CompetitionId,
                                season.FromYear,
                                season.UntilYear,
                                season.Introduction,
                                season.EnableTournaments,
                                season.PlayersPerTeam,
                                season.Overs,
                                season.EnableLastPlayerBatsOn,
                                ResultsTableType = season.ResultsTableType.ToString(),
                                season.EnableRunsScored,
                                season.EnableRunsConceded,
                                season.Results,
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
                        var pointsRules = (await connection.QueryAsync<PointsRule>(
                            $@"SELECT MatchResultType, HomePoints, AwayPoints FROM { Tables.SeasonPointsRule } WHERE SeasonId = 
                                (
                                    SELECT TOP 1 SeasonId FROM StoolballSeason WHERE CompetitionId = @CompetitionId AND FromYear < @FromYear ORDER BY FromYear DESC
                                )",
                                new
                                {
                                    season.Competition.CompetitionId,
                                    season.FromYear
                                },
                                transaction).ConfigureAwait(false)).ToList();

                        // If there are none, start with some default points rules
                        if (pointsRules.Count == 0)
                        {
                            pointsRules.AddRange(new PointsRule[] {
                                new PointsRule{ MatchResultType = MatchResultType.HomeWin, HomePoints = 2, AwayPoints = 0 },
                                new PointsRule{ MatchResultType = MatchResultType.AwayWin, HomePoints = 0, AwayPoints = 2 },
                                new PointsRule{ MatchResultType = MatchResultType.HomeWinByForfeit, HomePoints = 2, AwayPoints = 0 },
                                new PointsRule{ MatchResultType = MatchResultType.AwayWinByForfeit, HomePoints = 0, AwayPoints = 2 },
                                new PointsRule{ MatchResultType = MatchResultType.Tie, HomePoints = 1, AwayPoints = 1 },
                                new PointsRule{ MatchResultType = MatchResultType.Cancelled, HomePoints = 1, AwayPoints = 1 },
                                new PointsRule{ MatchResultType = MatchResultType.AbandonedDuringPlayAndCancelled, HomePoints = 1, AwayPoints = 1 }
                            });
                        }

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
                                    SELECT TOP 1 SeasonId FROM { Tables.Season } WHERE CompetitionId = @CompetitionId AND FromYear < @FromYear ORDER BY FromYear DESC
                                )
                                AND 
                                st.WithdrawnDate IS NULL
                                AND (t.UntilYear IS NULL OR t.UntilYear <= @FromYear)",
                            new
                            {
                                season.Competition.CompetitionId,
                                season.FromYear
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
                season.Results = _htmlSanitiser.Sanitize(season.Results);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {

                        await connection.ExecuteAsync(
                            $@"UPDATE {Tables.Season} SET
                                Introduction = @Introduction,
                                EnableTournaments = @EnableTournaments,
                                PlayersPerTeam = @PlayersPerTeam,
                                Overs = @Overs,
                                EnableLastPlayerBatsOn = @EnableLastPlayerBatsOn,
                                ResultsTableType = @ResultsTableType,
                                EnableRunsScored = @EnableRunsScored,
                                EnableRunsConceded = @EnableRunsConceded,
                                Results = @Results
						        WHERE SeasonId = @SeasonId",
                            new
                            {
                                season.Introduction,
                                season.EnableTournaments,
                                season.PlayersPerTeam,
                                season.Overs,
                                season.EnableLastPlayerBatsOn,
                                ResultsTableType = season.ResultsTableType.ToString(),
                                season.EnableRunsScored,
                                season.EnableRunsConceded,
                                season.Results,
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

        /// <summary>
        /// Updates league points settings for a stoolball season
        /// </summary>
        public async Task<Season> UpdatePoints(Season season, Guid memberKey, string memberName)
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
                season.Results = _htmlSanitiser.Sanitize(season.Results);

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        foreach (var rule in season.PointsRules)
                        {
                            await connection.ExecuteAsync($@"UPDATE {Tables.SeasonPointsRule} SET 
                                    HomePoints = @HomePoints, 
                                    AwayPoints = @AwayPoints 
                                    WHERE SeasonPointsRuleId = @PointsRuleId",
                                    new
                                    {
                                        rule.HomePoints,
                                        rule.AwayPoints,
                                        rule.PointsRuleId
                                    },
                                    transaction).ConfigureAwait(false);
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


        /// <summary>
        /// Updates teams in a stoolball season
        /// </summary>
        public async Task<Season> UpdateTeams(Season season, Guid memberKey, string memberName)
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
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        await connection.ExecuteAsync($"DELETE FROM {Tables.SeasonTeam} WHERE SeasonId = @SeasonId", new { season.SeasonId }, transaction).ConfigureAwait(false);
                        foreach (var team in season.Teams)
                        {
                            await connection.ExecuteAsync($@"INSERT INTO {Tables.SeasonTeam} 
                                    (SeasonTeamId, SeasonId, TeamId, WithdrawnDate) 
                                    VALUES (@SeasonTeamId, @SeasonId, @TeamId, @WithdrawnDate)",
                                    new
                                    {
                                        SeasonTeamId = Guid.NewGuid(),
                                        season.SeasonId,
                                        team.Team.TeamId,
                                        team.WithdrawnDate
                                    },
                                    transaction).ConfigureAwait(false);
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

        /// <summary>
        /// Deletes a stoolball season
        /// </summary>
        public async Task DeleteSeason(Season season, Guid memberKey, string memberName)
        {
            if (season is null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        await connection.ExecuteAsync($"DELETE FROM {Tables.SeasonTeam} WHERE SeasonId = @SeasonId", new { season.SeasonId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.SeasonPointsRule} WHERE SeasonId = @SeasonId", new { season.SeasonId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.SeasonPointsAdjustment} WHERE SeasonId = @SeasonId", new { season.SeasonId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.SeasonMatchType} WHERE SeasonId = @SeasonId", new { season.SeasonId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentSeason} WHERE SeasonId = @SeasonId", new { season.SeasonId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.Season} WHERE SeasonId = @SeasonId", new { season.SeasonId }, transaction).ConfigureAwait(false);
                        transaction.Commit();
                    }
                }

                await _redirectsRepository.DeleteRedirectsByDestinationPrefix(season.SeasonRoute).ConfigureAwait(false);

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Delete,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = season.EntityUri,
                    State = JsonConvert.SerializeObject(season),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error<SqlServerSeasonRepository>(e);
                throw;
            }
        }
    }
}
