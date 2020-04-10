using Newtonsoft.Json;
using Stoolball.Clubs;
using Stoolball.Umbraco.Data.Audit;
using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using static Stoolball.Umbraco.Data.Constants;

namespace Stoolball.Umbraco.Data.Clubs
{
    /// <summary>
    /// Writes stoolball club data to the Umbraco database
    /// </summary>
    public class SqlServerClubRepository : IClubRepository
    {
        private readonly IScopeProvider _scopeProvider;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;

        public SqlServerClubRepository(IScopeProvider scopeProvider, IAuditRepository auditRepository, ILogger logger)
        {
            _scopeProvider = scopeProvider ?? throw new System.ArgumentNullException(nameof(scopeProvider));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a stoolball club and populates the <see cref="Club.ClubId"/>
        /// </summary>
        public async Task<Club> CreateClub(Club club)
        {
            if (club is null)
            {
                throw new ArgumentNullException(nameof(club));
            }

            try
            {
                using (var scope = _scopeProvider.CreateScope())
                {
                    using (var transaction = scope.Database.GetTransaction())
                    {
                        await scope.Database.ExecuteAsync($@"INSERT INTO {Tables.Club}
						(PlaysOutdoors, PlaysIndoors, Twitter, Facebook, Instagram, ClubMark, HowManyPlayers, ClubRoute)
						VALUES (@0, @1, @2, @3, @4, @5, @6, @7)",
                            club.PlaysOutdoors,
                            club.PlaysIndoors,
                            club.Twitter,
                            club.Facebook,
                            club.Instagram,
                            club.ClubMark,
                            club.HowManyPlayers,
                            club.ClubRoute).ConfigureAwait(false);
                        club.ClubId = await scope.Database.ExecuteScalarAsync<int>("SELECT SCOPE_IDENTITY()").ConfigureAwait(false);
                        await scope.Database.ExecuteAsync($@"INSERT INTO {Tables.ClubName} 
							(ClubId, ClubName, FromDate) VALUES (@0, @1, @2)",
                            club.ClubId,
                            club.ClubName,
                            DateTime.UtcNow
                            ).ConfigureAwait(false);
                        transaction.Complete();
                    }
                    scope.Complete();
                }

                await _auditRepository.CreateAudit(new AuditRecord
                {
                    Action = AuditAction.Create,
                    ActorName = nameof(SqlServerClubRepository),
                    EntityUri = club.EntityUri,
                    State = JsonConvert.SerializeObject(club),
                    AuditDate = DateTime.UtcNow
                }).ConfigureAwait(false);

                return club;
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerClubRepository), ex);
                throw;
            }
        }
    }
}
