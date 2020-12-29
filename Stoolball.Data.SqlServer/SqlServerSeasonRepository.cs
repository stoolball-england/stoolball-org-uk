using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Competitions;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Teams;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
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
        private readonly IDataRedactor _dataRedactor;

        public SqlServerSeasonRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger,
            IHtmlSanitizer htmlSanitiser, IRedirectsRepository redirectsRepository, IDataRedactor dataRedactor)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _htmlSanitiser = htmlSanitiser ?? throw new ArgumentNullException(nameof(htmlSanitiser));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _dataRedactor = dataRedactor ?? throw new ArgumentNullException(nameof(dataRedactor));
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

        private static Season CreateAuditableCopy(Season season)
        {
            return new Season
            {
                SeasonId = season.SeasonId,
                FromYear = season.FromYear,
                UntilYear = season.UntilYear,
                Competition = new Competition
                {
                    CompetitionId = season.Competition?.CompetitionId,
                    CompetitionRoute = season.Competition?.CompetitionRoute
                },
                Introduction = season.Introduction,
                MatchTypes = season.MatchTypes,
                EnableTournaments = season.EnableTournaments,
                EnableBonusOrPenaltyRuns = season.EnableBonusOrPenaltyRuns,
                PlayersPerTeam = season.PlayersPerTeam,
                Overs = season.Overs,
                EnableLastPlayerBatsOn = season.EnableLastPlayerBatsOn,
                PointsRules = season.PointsRules,
                ResultsTableType = season.ResultsTableType,
                EnableRunsScored = season.EnableRunsScored,
                EnableRunsConceded = season.EnableRunsConceded,
                Teams = season.Teams.Select(x => new TeamInSeason { WithdrawnDate = x.WithdrawnDate, Team = new Team { TeamId = x.Team.TeamId } }).ToList(),
                Results = season.Results,
                SeasonRoute = season.SeasonRoute
            };
        }

        private Season CreateRedactedCopy(Season season)
        {
            var redacted = CreateAuditableCopy(season);
            redacted.Introduction = _dataRedactor.RedactPersonalData(redacted.Introduction);
            redacted.Results = _dataRedactor.RedactPersonalData(redacted.Results);
            return redacted;
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

            var auditableSeason = CreateAuditableCopy(season);
            auditableSeason.SeasonId = Guid.NewGuid();
            auditableSeason.Introduction = _htmlSanitiser.Sanitize(auditableSeason.Introduction);
            auditableSeason.Results = _htmlSanitiser.Sanitize(auditableSeason.Results);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    auditableSeason.SeasonRoute = $"{auditableSeason.Competition.CompetitionRoute}/{auditableSeason.FromYear}";
                    if (auditableSeason.UntilYear > auditableSeason.FromYear)
                    {
                        auditableSeason.SeasonRoute = $"{auditableSeason.SeasonRoute}-{auditableSeason.UntilYear.ToString(CultureInfo.InvariantCulture).Substring(2)}";
                    }

                    await connection.ExecuteAsync(
                        $@"INSERT INTO {Tables.Season} (SeasonId, CompetitionId, FromYear, UntilYear, Introduction, EnableTournaments, EnableBonusOrPenaltyRuns,
                                PlayersPerTeam, Overs, EnableLastPlayerBatsOn, ResultsTableType, EnableRunsScored, EnableRunsConceded, Results, SeasonRoute) 
                                VALUES 
                                (@SeasonId, @CompetitionId, @FromYear, @UntilYear, @Introduction, @EnableTournaments, @EnableBonusOrPenaltyRuns, @PlayersPerTeam, @Overs,
                                 @EnableLastPlayerBatsOn, @ResultsTableType, @EnableRunsScored, @EnableRunsConceded, @Results, @SeasonRoute)",
                        new
                        {
                            auditableSeason.SeasonId,
                            auditableSeason.Competition.CompetitionId,
                            auditableSeason.FromYear,
                            auditableSeason.UntilYear,
                            auditableSeason.Introduction,
                            auditableSeason.EnableTournaments,
                            auditableSeason.EnableBonusOrPenaltyRuns,
                            auditableSeason.PlayersPerTeam,
                            auditableSeason.Overs,
                            auditableSeason.EnableLastPlayerBatsOn,
                            ResultsTableType = auditableSeason.ResultsTableType.ToString(),
                            auditableSeason.EnableRunsScored,
                            auditableSeason.EnableRunsConceded,
                            auditableSeason.Results,
                            auditableSeason.SeasonRoute
                        }, transaction).ConfigureAwait(false);

                    foreach (var matchType in auditableSeason.MatchTypes)
                    {
                        await connection.ExecuteAsync($@"INSERT INTO {Tables.SeasonMatchType} 
                                       (SeasonMatchTypeId, SeasonId, MatchType) 
                                        VALUES (@SeasonMatchTypeId, @SeasonId, @MatchType)",
                                    new
                                    {
                                        SeasonMatchTypeId = Guid.NewGuid(),
                                        auditableSeason.SeasonId,
                                        MatchType = matchType.ToString()
                                    }, transaction).ConfigureAwait(false);
                    }

                    // Copy points rules from the most recent season
                    auditableSeason.PointsRules = (await connection.QueryAsync<PointsRule>(
                        $@"SELECT MatchResultType, HomePoints, AwayPoints FROM { Tables.SeasonPointsRule } WHERE SeasonId = 
                                (
                                    SELECT TOP 1 SeasonId FROM StoolballSeason WHERE CompetitionId = @CompetitionId AND FromYear < @FromYear ORDER BY FromYear DESC
                                )",
                            new
                            {
                                auditableSeason.Competition.CompetitionId,
                                auditableSeason.FromYear
                            },
                            transaction).ConfigureAwait(false)).ToList();

                    // If there are none, start with some default points rules
                    if (auditableSeason.PointsRules.Count == 0)
                    {
                        auditableSeason.PointsRules.AddRange(new PointsRule[] {
                                new PointsRule{ MatchResultType = MatchResultType.HomeWin, HomePoints = 2, AwayPoints = 0 },
                                new PointsRule{ MatchResultType = MatchResultType.AwayWin, HomePoints = 0, AwayPoints = 2 },
                                new PointsRule{ MatchResultType = MatchResultType.HomeWinByForfeit, HomePoints = 2, AwayPoints = 0 },
                                new PointsRule{ MatchResultType = MatchResultType.AwayWinByForfeit, HomePoints = 0, AwayPoints = 2 },
                                new PointsRule{ MatchResultType = MatchResultType.Tie, HomePoints = 1, AwayPoints = 1 },
                                new PointsRule{ MatchResultType = MatchResultType.Cancelled, HomePoints = 1, AwayPoints = 1 },
                                new PointsRule{ MatchResultType = MatchResultType.AbandonedDuringPlayAndCancelled, HomePoints = 1, AwayPoints = 1 }
                            });
                    }

                    foreach (var pointsRule in auditableSeason.PointsRules)
                    {
                        pointsRule.PointsRuleId = Guid.NewGuid();
                        await connection.ExecuteAsync($@"INSERT INTO { Tables.SeasonPointsRule } 
                                (SeasonPointsRuleId, SeasonId, MatchResultType, HomePoints, AwayPoints)
                                VALUES (@SeasonPointsRuleId, @SeasonId, @MatchResultType, @HomePoints, @AwayPoints)",
                            new
                            {
                                SeasonPointsRuleId = pointsRule.PointsRuleId,
                                auditableSeason.SeasonId,
                                pointsRule.MatchResultType,
                                pointsRule.HomePoints,
                                pointsRule.AwayPoints
                            },
                            transaction).ConfigureAwait(false);
                    }

                    // Copy teams from the most recent season, where the teams did not withdraw and were still active in the season being added
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
                            auditableSeason.Competition.CompetitionId,
                            auditableSeason.FromYear
                        },
                        transaction).ConfigureAwait(false);

                    foreach (var teamId in teamIds)
                    {
                        auditableSeason.Teams.Add(new TeamInSeason { Team = new Team { TeamId = teamId } });
                        await connection.ExecuteAsync($@"INSERT INTO { Tables.SeasonTeam } 
                                (SeasonTeamId, SeasonId, TeamId)
                                VALUES (@SeasonTeamId, @SeasonId, @TeamId)",
                            new
                            {
                                SeasonTeamId = Guid.NewGuid(),
                                auditableSeason.SeasonId,
                                teamId
                            },
                            transaction).ConfigureAwait(false);
                    }

                    var redacted = CreateRedactedCopy(auditableSeason);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Create,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableSeason.EntityUri,
                        State = JsonConvert.SerializeObject(auditableSeason),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Created, redacted, memberName, memberKey, GetType(), nameof(SqlServerSeasonRepository.CreateSeason));
                }
            }

            return auditableSeason;
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

            var auditableSeason = CreateAuditableCopy(season);
            auditableSeason.Introduction = _htmlSanitiser.Sanitize(auditableSeason.Introduction);
            auditableSeason.Results = _htmlSanitiser.Sanitize(auditableSeason.Results);

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
                                EnableBonusOrPenaltyRuns = @EnableBonusOrPenaltyRuns,
                                ResultsTableType = @ResultsTableType,
                                EnableRunsScored = @EnableRunsScored,
                                EnableRunsConceded = @EnableRunsConceded,
                                Results = @Results
						        WHERE SeasonId = @SeasonId",
                        new
                        {
                            auditableSeason.Introduction,
                            auditableSeason.EnableTournaments,
                            auditableSeason.PlayersPerTeam,
                            auditableSeason.Overs,
                            auditableSeason.EnableLastPlayerBatsOn,
                            auditableSeason.EnableBonusOrPenaltyRuns,
                            ResultsTableType = auditableSeason.ResultsTableType.ToString(),
                            auditableSeason.EnableRunsScored,
                            auditableSeason.EnableRunsConceded,
                            auditableSeason.Results,
                            auditableSeason.SeasonId
                        }, transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($@"DELETE FROM {Tables.SeasonMatchType} WHERE SeasonId = @SeasonId AND MatchType NOT IN @MatchTypes",
                        new
                        {
                            auditableSeason.SeasonId,
                            MatchTypes = auditableSeason.MatchTypes.Select(x => x.ToString())
                        },
                        transaction).ConfigureAwait(false);

                    var currentMatchTypes = (await connection.QueryAsync<string>($"SELECT MatchType FROM {Tables.SeasonMatchType} WHERE SeasonId = @SeasonId", new { auditableSeason.SeasonId }, transaction).ConfigureAwait(false)).ToList();
                    foreach (var matchType in auditableSeason.MatchTypes)
                    {
                        if (!currentMatchTypes.Contains(matchType.ToString()))
                        {
                            await connection.ExecuteAsync($@"INSERT INTO {Tables.SeasonMatchType} 
                                       (SeasonMatchTypeId, SeasonId, MatchType) 
                                        VALUES (@SeasonMatchTypeId, @SeasonId, @MatchType)",
                                    new
                                    {
                                        SeasonMatchTypeId = Guid.NewGuid(),
                                        auditableSeason.SeasonId,
                                        MatchType = matchType.ToString()
                                    }, transaction).ConfigureAwait(false);
                        }
                    }

                    var redacted = CreateRedactedCopy(auditableSeason);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableSeason.EntityUri,
                        State = JsonConvert.SerializeObject(auditableSeason),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerSeasonRepository.UpdateSeason));
                }
            }

            return auditableSeason;
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

            var auditableSeason = CreateAuditableCopy(season);
            auditableSeason.Results = _htmlSanitiser.Sanitize(auditableSeason.Results);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    foreach (var rule in auditableSeason.PointsRules)
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

                    var redacted = CreateRedactedCopy(auditableSeason);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableSeason.EntityUri,
                        State = JsonConvert.SerializeObject(auditableSeason),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerSeasonRepository.UpdatePoints));
                }
            }

            return auditableSeason;
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

            var auditableSeason = CreateAuditableCopy(season);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($"DELETE FROM {Tables.SeasonTeam} WHERE SeasonId = @SeasonId", new { auditableSeason.SeasonId }, transaction).ConfigureAwait(false);
                    foreach (var team in auditableSeason.Teams)
                    {
                        await connection.ExecuteAsync($@"INSERT INTO {Tables.SeasonTeam} 
                                    (SeasonTeamId, SeasonId, TeamId, WithdrawnDate) 
                                    VALUES (@SeasonTeamId, @SeasonId, @TeamId, @WithdrawnDate)",
                                new
                                {
                                    SeasonTeamId = Guid.NewGuid(),
                                    auditableSeason.SeasonId,
                                    team.Team.TeamId,
                                    team.WithdrawnDate
                                },
                                transaction).ConfigureAwait(false);
                    }

                    var redacted = CreateRedactedCopy(auditableSeason);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableSeason.EntityUri,
                        State = JsonConvert.SerializeObject(auditableSeason),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerSeasonRepository.UpdateTeams));
                }
            }

            return auditableSeason;
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

            var auditableSeason = CreateAuditableCopy(season);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($"DELETE FROM {Tables.SeasonTeam} WHERE SeasonId = @SeasonId", new { auditableSeason.SeasonId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.SeasonPointsRule} WHERE SeasonId = @SeasonId", new { auditableSeason.SeasonId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.SeasonPointsAdjustment} WHERE SeasonId = @SeasonId", new { auditableSeason.SeasonId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.SeasonMatchType} WHERE SeasonId = @SeasonId", new { auditableSeason.SeasonId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentSeason} WHERE SeasonId = @SeasonId", new { auditableSeason.SeasonId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Season} WHERE SeasonId = @SeasonId", new { auditableSeason.SeasonId }, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix(auditableSeason.SeasonRoute, transaction).ConfigureAwait(false);

                    var redacted = CreateRedactedCopy(auditableSeason);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Delete,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableSeason.EntityUri,
                        State = JsonConvert.SerializeObject(auditableSeason),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(GetType(), LoggingTemplates.Deleted, redacted, memberName, memberKey, GetType(), nameof(SqlServerSeasonRepository.DeleteSeason));
                }
            }
        }
    }
}
