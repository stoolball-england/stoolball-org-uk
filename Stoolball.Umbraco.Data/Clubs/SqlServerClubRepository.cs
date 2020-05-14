using Newtonsoft.Json;
using Stoolball.Audit;
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
                        club.ClubId = Guid.NewGuid();

                        await scope.Database.ExecuteAsync($@"INSERT INTO {Tables.Club}
						(ClubId, Twitter, Facebook, Instagram, ClubMark, ClubRoute)
						VALUES (@0, @1, @2, @3, @4, @5)",
                            club.ClubId,
                            PrefixAtSign(club.Twitter),
                            PrefixUrlProtocol(club.Facebook),
                            PrefixAtSign(club.Instagram),
                            club.ClubMark,
                            club.ClubRoute).ConfigureAwait(false);
                        await scope.Database.ExecuteAsync($@"INSERT INTO {Tables.ClubName} 
							(ClubNameId, ClubId, ClubName, FromDate) VALUES (@0, @1, @2, @3)",
                            Guid.NewGuid(),
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


        /// <summary>
        /// Updates a stoolball club
        /// </summary>
        public async Task<Club> UpdateClub(Club club)
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
                        await scope.Database.ExecuteAsync(
                            $@"UPDATE {Tables.Club} SET
                                ClubMark = @0,
                                Facebook = @1,
                                Twitter = @2,
                                Instagram = @3,
                                YouTube = @4,
                                Website = @5
						        WHERE ClubId = @6",
                            club.ClubMark,
                            PrefixUrlProtocol(club.Facebook),
                            PrefixAtSign(club.Twitter),
                            PrefixAtSign(club.Instagram),
                            PrefixUrlProtocol(club.YouTube),
                            PrefixUrlProtocol(club.Website),
                            club.ClubId).ConfigureAwait(false);

                        var currentName = await scope.Database.ExecuteScalarAsync<string>($"SELECT ClubName FROM {Tables.ClubName} WHERE ClubId = @0 AND UntilDate IS NULL", club.ClubId).ConfigureAwait(false);
                        if (club.ClubName?.Trim() != currentName?.Trim())
                        {
                            await scope.Database.ExecuteAsync($"UPDATE {Tables.ClubName} SET UntilDate = GETUTCDATE() WHERE ClubId = @0 AND UntilDate IS NULL", club.ClubId).ConfigureAwait(false);
                            await scope.Database.ExecuteAsync($@"INSERT INTO {Tables.ClubName} 
                                (ClubNameId, ClubId, ClubName, FromDate) VALUES (@0, @1, @2, GETUTCDATE())",
                                Guid.NewGuid(),
                                club.ClubId,
                                club.ClubName
                                ).ConfigureAwait(false);
                        }

                        transaction.Complete();
                    }
                    scope.Complete();
                }

                /* await _auditRepository.CreateAudit(new AuditRecord
                 {
                     Action = AuditAction.Create,
                     ActorName = nameof(SqlServerClubRepository),
                     EntityUri = club.EntityUri,
                     State = JsonConvert.SerializeObject(club),
                     AuditDate = DateTime.UtcNow
                 }).ConfigureAwait(false);*/

                return club;
            }
            catch (Exception ex)
            {
                _logger.Error(typeof(SqlServerClubRepository), ex);
                throw;
            }
        }

        private string PrefixUrlProtocol(string url)
        {
            url = url?.Trim();
            if (!string.IsNullOrEmpty(url) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url;
            }
            return url;
        }

        private string PrefixAtSign(string account)
        {
            account = account?.Trim();
            if (!string.IsNullOrEmpty(account) && !account.StartsWith("@", StringComparison.OrdinalIgnoreCase))
            {
                account = "@" + account;
            }
            return account;
        }
    }
}
