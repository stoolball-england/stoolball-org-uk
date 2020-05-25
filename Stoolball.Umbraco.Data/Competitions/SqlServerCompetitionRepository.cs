using Dapper;
using Newtonsoft.Json;
using Stoolball.Audit;
using Stoolball.Competitions;
using Stoolball.Routing;
using Stoolball.Umbraco.Data.Audit;
using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Competitions
{
    /// <summary>
    /// Writes stoolball competition data to the Umbraco database
    /// </summary>
    public class SqlServerCompetitionRepository : ICompetitionRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRedirectsRepository _redirectsRepository;

        public SqlServerCompetitionRepository(IDatabaseConnectionFactory databaseConnectionFactory, IAuditRepository auditRepository, ILogger logger, IRouteGenerator routeGenerator, IRedirectsRepository redirectsRepository)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
        }

        /// <summary>
        /// Creates a stoolball competition and populates the <see cref="Competition.CompetitionId"/>
        /// </summary>
        /// <returns>The created competition</returns>
        public async Task<Competition> CreateCompetition(Competition competition, Guid memberKey, string memberName)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            try
            {
                competition.CompetitionId = Guid.NewGuid();

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        competition.CompetitionRoute = _routeGenerator.GenerateRoute("/competitions", competition.CompetitionName, NoiseWords.CompetitionRoute);
                        int count;
                        do
                        {
                            count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Competition} WHERE CompetitionRoute = @CompetitionRoute", new { competition.CompetitionRoute }, transaction).ConfigureAwait(false);
                            if (count > 0)
                            {
                                competition.CompetitionRoute = _routeGenerator.IncrementRoute(competition.CompetitionRoute);
                            }
                        }
                        while (count > 0);

                        await connection.ExecuteAsync(
                            $@"INSERT INTO {Tables.Competition} (CompetitionId, CompetitionName, CompetitionRoute, MemberGroupId, MemberGroupName) 
                                VALUES (@CompetitionId, @CompetitionName, @CompetitionRoute, @MemberGroupId, @MemberGroupName)",
                            new
                            {
                                competition.CompetitionId,
                                competition.CompetitionName,
                                competition.CompetitionRoute,
                                competition.MemberGroupId,
                                competition.MemberGroupName
                            }, transaction).ConfigureAwait(false);

                        transaction.Commit();
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Create,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = competition.EntityUri,
                    State = JsonConvert.SerializeObject(competition),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerCompetitionRepository), ex);
            }

            return competition;
        }


        /// <summary>
        /// Updates a stoolball competition
        /// </summary>
        public async Task<Competition> UpdateCompetition(Competition competition, Guid memberKey, string memberName)
        {
            if (competition is null)
            {
                throw new ArgumentNullException(nameof(competition));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            try
            {
                string routeBeforeUpdate = competition.CompetitionRoute;

                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {

                        competition.CompetitionRoute = _routeGenerator.GenerateRoute("/competitions", competition.CompetitionName, NoiseWords.CompetitionRoute);
                        if (competition.CompetitionRoute != routeBeforeUpdate)
                        {
                            int count;
                            do
                            {
                                count = await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Competition} WHERE CompetitionRoute = @CompetitionRoute", new { competition.CompetitionRoute }, transaction).ConfigureAwait(false);
                                if (count > 0)
                                {
                                    competition.CompetitionRoute = _routeGenerator.IncrementRoute(competition.CompetitionRoute);
                                }
                            }
                            while (count > 0);
                        }

                        await connection.ExecuteAsync(
                            $@"UPDATE {Tables.Competition} SET
                                CompetitionName = @CompetitionName,
                                CompetitionRoute = @CompetitionRoute
						        WHERE CompetitionId = @CompetitionId",
                            new
                            {
                                competition.CompetitionName,
                                competition.CompetitionRoute,
                                competition.CompetitionId
                            }, transaction).ConfigureAwait(false);

                        transaction.Commit();
                    }

                    if (routeBeforeUpdate != competition.CompetitionRoute)
                    {
                        await _redirectsRepository.InsertRedirect(routeBeforeUpdate, competition.CompetitionRoute, null).ConfigureAwait(false);
                    }
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Update,
                    MemberKey = memberKey,
                    ActorName = memberName,
                    EntityUri = competition.EntityUri,
                    State = JsonConvert.SerializeObject(competition),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);

            }
            catch (SqlException ex)
            {
                _logger.Error(typeof(SqlServerCompetitionRepository), ex);
            }

            return competition;
        }
    }
}
