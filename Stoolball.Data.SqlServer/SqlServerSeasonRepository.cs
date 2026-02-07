using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Ganss.Xss;
using Newtonsoft.Json;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Logging;
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
        private readonly ILogger<SqlServerSeasonRepository> _logger;
        private readonly IHtmlSanitizer _htmlSanitiser;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly IStoolballEntityCopier _copier;

        public SqlServerSeasonRepository(
            IDatabaseConnectionFactory databaseConnectionFactory,
            IAuditRepository auditRepository,
            ILogger<SqlServerSeasonRepository> logger,
            IHtmlSanitizer htmlSanitiser,
            IRedirectsRepository redirectsRepository,
            IStoolballEntityCopier copier)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _htmlSanitiser = htmlSanitiser ?? throw new ArgumentNullException(nameof(htmlSanitiser));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _copier = copier ?? throw new ArgumentNullException(nameof(copier));
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

            if (season.Competition?.CompetitionId is null)
            {
                throw new ArgumentException($"{nameof(season.Competition.CompetitionId)} cannot be null", nameof(season));
            }

            if (memberKey == default)
            {
                throw new ArgumentNullException(nameof(memberKey));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableSeason = _copier.CreateAuditableCopy(season);
            auditableSeason.SeasonId = Guid.NewGuid();
            auditableSeason.Introduction = auditableSeason.Introduction is not null ? _htmlSanitiser.Sanitize(auditableSeason.Introduction) : null;
            auditableSeason.Results = auditableSeason.Results is not null ? _htmlSanitiser.Sanitize(auditableSeason.Results) : null;

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    auditableSeason.SeasonRoute = $"{auditableSeason.Competition!.CompetitionRoute}/{auditableSeason.FromYear}";
                    if (auditableSeason.UntilYear > auditableSeason.FromYear)
                    {
                        auditableSeason.SeasonRoute = $"{auditableSeason.SeasonRoute}-{auditableSeason.UntilYear.ToString(CultureInfo.InvariantCulture).Substring(2)}";
                    }

                    // Get the most recent season, if any, to copy existing settings as defaults
                    var previousSeason = await connection.QuerySingleOrDefaultAsync<Season>(
                        $"SELECT TOP 1 SeasonId, ResultsTableType, EnableRunsScored, EnableRunsConceded FROM {Tables.Season} WHERE CompetitionId = @CompetitionId AND FromYear < @FromYear ORDER BY FromYear DESC",
                        new
                        {
                            auditableSeason.Competition.CompetitionId,
                            auditableSeason.FromYear
                        }, transaction).ConfigureAwait(false);

                    if (previousSeason is not null)
                    {
                        auditableSeason.ResultsTableType = previousSeason.ResultsTableType;
                        auditableSeason.EnableRunsScored = previousSeason.EnableRunsScored;
                        auditableSeason.EnableRunsConceded = previousSeason.EnableRunsConceded;
                    }
                    else
                    {
                        auditableSeason.ResultsTableType = Defaults.ResultsTable.TableType;
                        auditableSeason.EnableRunsScored = Defaults.ResultsTable.EnableRunsScored;
                        auditableSeason.EnableRunsConceded = Defaults.ResultsTable.EnableRunsConceded;
                    }

                    await connection.ExecuteAsync(
                        $@"INSERT INTO {Tables.Season} (SeasonId, CompetitionId, FromYear, UntilYear, Introduction, EnableTournaments, EnableBonusOrPenaltyRuns,
                                PlayersPerTeam, EnableLastPlayerBatsOn, ResultsTableType, EnableRunsScored, EnableRunsConceded, Results, SeasonRoute) 
                                VALUES 
                                (@SeasonId, @CompetitionId, @FromYear, @UntilYear, @Introduction, @EnableTournaments, @EnableBonusOrPenaltyRuns, 
                                 @PlayersPerTeam, @EnableLastPlayerBatsOn, @ResultsTableType, @EnableRunsScored, @EnableRunsConceded, @Results, @SeasonRoute)",
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
                            auditableSeason.EnableLastPlayerBatsOn,
                            ResultsTableType = auditableSeason.ResultsTableType.ToString(),
                            auditableSeason.EnableRunsScored,
                            auditableSeason.EnableRunsConceded,
                            auditableSeason.Results,
                            auditableSeason.SeasonRoute
                        }, transaction).ConfigureAwait(false);

                    await InsertOverSets(auditableSeason, transaction).ConfigureAwait(false);

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
                    if (previousSeason != null)
                    {
                        auditableSeason.PointsRules = (await connection.QueryAsync<PointsRule>(
                            $@"SELECT MatchResultType, HomePoints, AwayPoints FROM {Tables.PointsRule} WHERE SeasonId = @SeasonId",
                                new
                                {
                                    previousSeason.SeasonId
                                },
                                transaction).ConfigureAwait(false)).ToList();
                    }

                    // If there are none, start with some default points rules
                    if (auditableSeason.PointsRules.Count == 0)
                    {
                        auditableSeason.PointsRules.AddRange(Defaults.PointsRules);
                    }

                    foreach (var pointsRule in auditableSeason.PointsRules)
                    {
                        pointsRule.PointsRuleId = Guid.NewGuid();
                        await connection.ExecuteAsync($@"INSERT INTO {Tables.PointsRule} 
                                (PointsRuleId, SeasonId, MatchResultType, HomePoints, AwayPoints)
                                VALUES (@PointsRuleId, @SeasonId, @MatchResultType, @HomePoints, @AwayPoints)",
                            new
                            {
                                pointsRule.PointsRuleId,
                                auditableSeason.SeasonId,
                                pointsRule.MatchResultType,
                                pointsRule.HomePoints,
                                pointsRule.AwayPoints
                            },
                            transaction).ConfigureAwait(false);
                    }

                    // Copy teams from the most recent season, where the teams did not withdraw and were still active in the season being added
                    auditableSeason.Teams.Clear();
                    if (previousSeason != null)
                    {
                        var teamIds = await connection.QueryAsync<Guid>(
                            $@"SELECT DISTINCT t.TeamId 
                                FROM {Tables.SeasonTeam} st 
                                INNER JOIN {Tables.Team} t ON st.TeamId = t.TeamId 
                                INNER JOIN {Tables.TeamVersion} tv ON t.TeamId = tv.TeamId
                                WHERE st.SeasonId = @SeasonId
                                AND 
                                st.WithdrawnDate IS NULL
                                AND (tv.UntilDate IS NULL OR tv.UntilDate >= @FromDate)",
                            new
                            {
                                previousSeason.SeasonId,
                                FromDate = new DateTime(auditableSeason.FromYear, 1, 1).ToUniversalTime()
                            },
                            transaction).ConfigureAwait(false);

                        foreach (var teamId in teamIds)
                        {
                            auditableSeason.Teams.Add(new TeamInSeason { Team = new Team { TeamId = teamId }, Season = new Season { SeasonId = auditableSeason.SeasonId } });
                            await connection.ExecuteAsync($@"INSERT INTO {Tables.SeasonTeam} 
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
                    }

                    var redacted = _copier.CreateRedactedCopy(auditableSeason);
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

                    _logger.Info(LoggingTemplates.Created, redacted, memberName, memberKey, GetType(), nameof(SqlServerSeasonRepository.CreateSeason));
                }
            }

            return auditableSeason;
        }

        private static async Task InsertOverSets(Season auditableSeason, IDbTransaction transaction)
        {
            for (var i = 0; i < auditableSeason.DefaultOverSets.Count; i++)
            {
                auditableSeason.DefaultOverSets[i].OverSetId = auditableSeason.DefaultOverSets[i].OverSetId ?? Guid.NewGuid();
                await transaction.Connection.ExecuteAsync($"INSERT INTO {Tables.OverSet} (OverSetId, SeasonId, OverSetNumber, Overs, BallsPerOver) VALUES (@OverSetId, @SeasonId, @OverSetNumber, @Overs, @BallsPerOver)",
                    new
                    {
                        auditableSeason.DefaultOverSets[i].OverSetId,
                        auditableSeason.SeasonId,
                        OverSetNumber = i + 1,
                        auditableSeason.DefaultOverSets[i].Overs,
                        auditableSeason.DefaultOverSets[i].BallsPerOver
                    },
                    transaction).ConfigureAwait(false);
            }
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

            if (memberKey == default)
            {
                throw new ArgumentNullException(nameof(memberKey));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableSeason = _copier.CreateAuditableCopy(season);
            auditableSeason.Introduction = auditableSeason.Introduction is not null ? _htmlSanitiser.Sanitize(auditableSeason.Introduction) : null;
            auditableSeason.Results = auditableSeason.Results is not null ? _htmlSanitiser.Sanitize(auditableSeason.Results) : null;

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
                                EnableLastPlayerBatsOn = @EnableLastPlayerBatsOn,
                                EnableBonusOrPenaltyRuns = @EnableBonusOrPenaltyRuns,
                                Results = @Results
						        WHERE SeasonId = @SeasonId",
                        new
                        {
                            auditableSeason.Introduction,
                            auditableSeason.EnableTournaments,
                            auditableSeason.PlayersPerTeam,
                            auditableSeason.EnableLastPlayerBatsOn,
                            auditableSeason.EnableBonusOrPenaltyRuns,
                            auditableSeason.Results,
                            auditableSeason.SeasonId
                        }, transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($"DELETE FROM {Tables.OverSet} WHERE SeasonId = @SeasonId", new { auditableSeason.SeasonId }, transaction).ConfigureAwait(false);
                    await InsertOverSets(auditableSeason, transaction).ConfigureAwait(false);

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

                    var redacted = _copier.CreateRedactedCopy(auditableSeason);
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

                    _logger.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerSeasonRepository.UpdateSeason));
                }
            }

            return auditableSeason;
        }

        /// <summary>
        /// Updates league points settings for a stoolball season
        /// </summary>
        public async Task<Season> UpdateResultsTable(Season season, Guid memberKey, string memberName)
        {
            if (season is null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            if (memberKey == default)
            {
                throw new ArgumentNullException(nameof(memberKey));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableSeason = _copier.CreateAuditableCopy(season);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync(
                    $@"UPDATE {Tables.Season} SET
                            ResultsTableType = @ResultsTableType,
                            EnableRunsScored = @EnableRunsScored,
                            EnableRunsConceded = @EnableRunsConceded
						    WHERE SeasonId = @SeasonId",
                    new
                    {
                        ResultsTableType = auditableSeason.ResultsTableType.ToString(),
                        auditableSeason.EnableRunsScored,
                        auditableSeason.EnableRunsConceded,
                        auditableSeason.SeasonId
                    }, transaction).ConfigureAwait(false);

                    foreach (var rule in auditableSeason.PointsRules)
                    {
                        await connection.ExecuteAsync($@"UPDATE {Tables.PointsRule} SET 
                                    HomePoints = @HomePoints, 
                                    AwayPoints = @AwayPoints 
                                    WHERE PointsRuleId = @PointsRuleId",
                                new
                                {
                                    rule.HomePoints,
                                    rule.AwayPoints,
                                    rule.PointsRuleId
                                },
                                transaction).ConfigureAwait(false);
                    }

                    var redacted = _copier.CreateRedactedCopy(auditableSeason);
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

                    _logger.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerSeasonRepository.UpdateResultsTable));
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

            if (memberKey == default)
            {
                throw new ArgumentNullException(nameof(memberKey));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableSeason = _copier.CreateAuditableCopy(season);

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
                                    WithdrawnDate = team.WithdrawnDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(team.WithdrawnDate.Value.Date, TimeZoneInfo.FindSystemTimeZoneById(UkTimeZone())) : (DateTime?)null
                                },
                                transaction).ConfigureAwait(false);
                    }

                    var redacted = _copier.CreateRedactedCopy(auditableSeason);
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

                    _logger.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(SqlServerSeasonRepository.UpdateTeams));
                }
            }

            return auditableSeason;
        }

        /// <inheritdoc />
        public async Task DeleteSeason(Season season, Guid memberKey, string memberName)
        {
            if (season is null)
            {
                throw new ArgumentNullException(nameof(season));
            }

            if (memberKey == default)
            {
                throw new ArgumentNullException(nameof(memberKey));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException($"'{nameof(memberName)}' cannot be null or whitespace", nameof(memberName));
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await DeleteSeasons(new[] { season }, memberKey, memberName, transaction).ConfigureAwait(false);

                    transaction.Commit();
                }
            }
        }

        /// <inheritdoc />
        public async Task DeleteSeasons(IEnumerable<Season> seasons, Guid memberKey, string memberName, IDbTransaction transaction)
        {
            if (seasons is null)
            {
                throw new ArgumentNullException(nameof(seasons));
            }

            if (memberKey == default)
            {
                throw new ArgumentNullException(nameof(memberKey));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException($"'{nameof(memberName)}' cannot be null or whitespace", nameof(memberName));
            }

            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            var auditableSeasons = seasons.Select(x => _copier.CreateAuditableCopy(x));
            var seasonIds = auditableSeasons.Select(x => x.SeasonId).OfType<Guid>();

            await transaction.Connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET SeasonId = NULL WHERE SeasonId IN @seasonIds", new { seasonIds }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.SeasonTeam} WHERE SeasonId IN @seasonIds", new { seasonIds }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.PointsRule} WHERE SeasonId IN @seasonIds", new { seasonIds }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.PointsAdjustment} WHERE SeasonId IN @seasonIds", new { seasonIds }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.OverSet} WHERE SeasonId IN @seasonIds", new { seasonIds }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.SeasonMatchType} WHERE SeasonId IN @seasonIds", new { seasonIds }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.AwardedTo} WHERE SeasonId IN @seasonIds", new { seasonIds }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"UPDATE {Tables.Match} SET SeasonId = NULL WHERE SeasonId IN @seasonIds", new { seasonIds }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.TournamentSeason} WHERE SeasonId IN @seasonIds", new { seasonIds }, transaction).ConfigureAwait(false);
            await transaction.Connection.ExecuteAsync($"DELETE FROM {Tables.Season} WHERE SeasonId IN @seasonIds", new { seasonIds }, transaction).ConfigureAwait(false);

            foreach (var auditableSeason in auditableSeasons)
            {
                await _redirectsRepository.DeleteRedirectsByDestinationPrefix(auditableSeason.SeasonRoute, transaction).ConfigureAwait(false);

                var redacted = _copier.CreateRedactedCopy(auditableSeason);
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

                _logger.Info(LoggingTemplates.Deleted, redacted, memberName, memberKey, GetType(), nameof(DeleteSeasons));
            }
        }
    }
}
