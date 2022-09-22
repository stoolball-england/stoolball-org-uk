using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Ganss.XSS;
using Newtonsoft.Json;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Teams;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer
{
    /// <summary>
    /// Writes stoolball tournament data to the Umbraco database
    /// </summary>
    public class SqlServerTournamentRepository : ITournamentRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IDapperWrapper _dapperWrapper;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<SqlServerTournamentRepository> _logger;
        private readonly IRouteGenerator _routeGenerator;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly ITeamRepository _teamRepository;
        private readonly IMatchRepository _matchRepository;
        private readonly IHtmlSanitizer _htmlSanitiser;
        private readonly IStoolballEntityCopier _stoolballEntityCopier;

        public SqlServerTournamentRepository(IDatabaseConnectionFactory databaseConnectionFactory, IDapperWrapper dapperWrapper, IAuditRepository auditRepository, ILogger<SqlServerTournamentRepository> logger,
            IRouteGenerator routeGenerator, IRedirectsRepository redirectsRepository, ITeamRepository teamRepository, IMatchRepository matchRepository, IHtmlSanitizer htmlSanitiser, IStoolballEntityCopier stoolballEntityCopier)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _dapperWrapper = dapperWrapper ?? throw new ArgumentNullException(nameof(dapperWrapper));
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _routeGenerator = routeGenerator ?? throw new ArgumentNullException(nameof(routeGenerator));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _teamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            _matchRepository = matchRepository ?? throw new ArgumentNullException(nameof(matchRepository));
            _htmlSanitiser = htmlSanitiser ?? throw new ArgumentNullException(nameof(htmlSanitiser));
            _stoolballEntityCopier = stoolballEntityCopier ?? throw new ArgumentNullException(nameof(stoolballEntityCopier));
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
        /// Creates a stoolball tournament
        /// </summary>
        public async Task<Tournament> CreateTournament(Tournament tournament, Guid memberKey, string memberName)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableTournament = _stoolballEntityCopier.CreateAuditableCopy(tournament);
            auditableTournament.TournamentId = Guid.NewGuid();
            auditableTournament.TournamentNotes = _htmlSanitiser.Sanitize(auditableTournament.TournamentNotes);
            auditableTournament.MemberKey = memberKey;

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    auditableTournament.TournamentRoute = await _routeGenerator.GenerateUniqueRoute(
                        "/tournaments",
                        auditableTournament.TournamentName + " " + auditableTournament.StartTime.Date.ToString("dMMMyyyy", CultureInfo.CurrentCulture),
                        NoiseWords.TournamentRoute,
                        async route => await transaction.Connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Tournament} WHERE TournamentRoute = @TournamentRoute", new { TournamentRoute = route }, transaction).ConfigureAwait(false)
                    ).ConfigureAwait(false);

                    await _dapperWrapper.ExecuteAsync($@"INSERT INTO {Tables.Tournament}
						(TournamentId, TournamentName, MatchLocationId, PlayerType, PlayersPerTeam, 
						 QualificationType, StartTime, StartTimeIsKnown, TournamentNotes, TournamentRoute, MemberKey)
						VALUES (@TournamentId, @TournamentName, @MatchLocationId, @PlayerType, @PlayersPerTeam, 
                        @QualificationType, @StartTime, @StartTimeIsKnown, @TournamentNotes, @TournamentRoute, @MemberKey)",
                    new
                    {
                        auditableTournament.TournamentId,
                        auditableTournament.TournamentName,
                        auditableTournament.TournamentLocation?.MatchLocationId,
                        PlayerType = auditableTournament.PlayerType.ToString(),
                        auditableTournament.PlayersPerTeam,
                        QualificationType = auditableTournament.QualificationType.ToString(),
                        StartTime = TimeZoneInfo.ConvertTimeToUtc(auditableTournament.StartTime.DateTime, TimeZoneInfo.FindSystemTimeZoneById(UkTimeZone())),
                        auditableTournament.StartTimeIsKnown,
                        auditableTournament.TournamentNotes,
                        auditableTournament.TournamentRoute,
                        auditableTournament.MemberKey
                    }, transaction).ConfigureAwait(false);

                    await InsertOverSets(auditableTournament, transaction).ConfigureAwait(false);

                    foreach (var team in auditableTournament.Teams)
                    {
                        await _dapperWrapper.ExecuteAsync($@"INSERT INTO {Tables.TournamentTeam} 
								(TournamentTeamId, TournamentId, TeamId, TeamRole, PlayingAsTeamName) 
                                VALUES (@TournamentTeamId, @TournamentId, @TeamId, @TeamRole, @PlayingAsTeamName)",
                            new
                            {
                                TournamentTeamId = Guid.NewGuid(),
                                auditableTournament.TournamentId,
                                team.Team.TeamId,
                                TeamRole = team.TeamRole.ToString(),
                                team.PlayingAsTeamName
                            },
                            transaction).ConfigureAwait(false);
                    }

                    foreach (var season in auditableTournament.Seasons)
                    {
                        await _dapperWrapper.ExecuteAsync($@"INSERT INTO {Tables.TournamentSeason} 
								(TournamentSeasonId, TournamentId, SeasonId) 
                                VALUES (@TournamentSeasonId, @TournamentId, @SeasonId)",
                            new
                            {
                                TournamentSeasonId = Guid.NewGuid(),
                                auditableTournament.TournamentId,
                                season.SeasonId
                            },
                            transaction).ConfigureAwait(false);
                    }

                    var redacted = _stoolballEntityCopier.CreateRedactedCopy(auditableTournament);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Create,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableTournament.EntityUri,
                        State = JsonConvert.SerializeObject(auditableTournament),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    }, transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Created, redacted, memberName, memberKey, GetType(), nameof(CreateTournament));
                }
            }

            return auditableTournament;
        }

        private async Task InsertOverSets(Tournament auditableTournament, IDbTransaction transaction)
        {
            var matchInningsIds = await _dapperWrapper.QueryAsync<Guid>($"SELECT MatchInningsId FROM {Tables.MatchInnings} mi INNER JOIN {Tables.Match} m ON mi.MatchId = m.MatchId WHERE m.TournamentId = @TournamentId", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);

            for (var i = 0; i < auditableTournament.DefaultOverSets.Count; i++)
            {
                auditableTournament.DefaultOverSets[i].OverSetId = Guid.NewGuid();
                await _dapperWrapper.ExecuteAsync($"INSERT INTO {Tables.OverSet} (OverSetId, TournamentId, OverSetNumber, Overs, BallsPerOver) VALUES (@OverSetId, @TournamentId, @OverSetNumber, @Overs, @BallsPerOver)",
                    new
                    {
                        auditableTournament.DefaultOverSets[i].OverSetId,
                        auditableTournament.TournamentId,
                        OverSetNumber = i + 1,
                        auditableTournament.DefaultOverSets[i].Overs,
                        auditableTournament.DefaultOverSets[i].BallsPerOver
                    },
                    transaction).ConfigureAwait(false);

                if (matchInningsIds != null)
                {
                    foreach (var matchInningsId in matchInningsIds)
                    {
                        await _dapperWrapper.ExecuteAsync($"INSERT INTO {Tables.OverSet} (OverSetId, MatchInningsId, OverSetNumber, Overs, BallsPerOver) VALUES (@OverSetId, @MatchInningsId, @OverSetNumber, @Overs, @BallsPerOver)",
                            new
                            {
                                OverSetId = Guid.NewGuid(),
                                MatchInningsId = matchInningsId,
                                OverSetNumber = i + 1,
                                auditableTournament.DefaultOverSets[i].Overs,
                                auditableTournament.DefaultOverSets[i].BallsPerOver
                            },
                            transaction).ConfigureAwait(false);
                    }
                }
            }
        }

        private class OverSetDto : OverSet
        {
            public Guid MatchInningsId { get; set; }
        }


        /// <summary>
        /// Updates a stoolball tournament
        /// </summary>
        public async Task<Tournament> UpdateTournament(Tournament tournament, Guid memberKey, string memberName)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableTournament = _stoolballEntityCopier.CreateAuditableCopy(tournament);
            auditableTournament.TournamentNotes = _htmlSanitiser.Sanitize(auditableTournament.TournamentNotes);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    auditableTournament.TournamentRoute = await _routeGenerator.GenerateUniqueRoute(
                        tournament.TournamentRoute,
                        "/tournaments",
                        auditableTournament.TournamentName + " " + auditableTournament.StartTime.Date.ToString("dMMMyyyy", CultureInfo.CurrentCulture),
                        NoiseWords.TournamentRoute,
                        async route => await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {Tables.Tournament} WHERE TournamentRoute = @TournamentRoute", new { TournamentRoute = route }, transaction).ConfigureAwait(false)
                    ).ConfigureAwait(false);

                    await connection.ExecuteAsync($@"UPDATE {Tables.Tournament} SET
						    TournamentName = @TournamentName,
                            MatchLocationId = @MatchLocationId, 
                            PlayerType = @PlayerType,
                            PlayersPerTeam = @PlayersPerTeam,
						    QualificationType = @QualificationType, 
                            StartTime = @StartTime, 
                            StartTimeIsKnown = @StartTimeIsKnown, 
                            TournamentNotes = @TournamentNotes, 
                            TournamentRoute = @TournamentRoute
                            WHERE TournamentId = @TournamentId",
                    new
                    {
                        auditableTournament.TournamentName,
                        auditableTournament.TournamentLocation?.MatchLocationId,
                        PlayerType = auditableTournament.PlayerType.ToString(),
                        auditableTournament.PlayersPerTeam,
                        QualificationType = auditableTournament.QualificationType.ToString(),
                        StartTime = TimeZoneInfo.ConvertTimeToUtc(auditableTournament.StartTime.DateTime, TimeZoneInfo.FindSystemTimeZoneById(UkTimeZone())),
                        auditableTournament.StartTimeIsKnown,
                        auditableTournament.TournamentNotes,
                        auditableTournament.TournamentRoute,
                        auditableTournament.TournamentId
                    }, transaction).ConfigureAwait(false);

                    await UpdateOverSets(auditableTournament, transaction);

                    // Set approximate start time based on 45 mins per match
                    await connection.ExecuteAsync($@"UPDATE {Tables.Match} SET
                            MatchLocationId = @MatchLocationId,
                            PlayerType = @PlayerType,
                            PlayersPerTeam = @PlayersPerTeam,
                            StartTime = DATEADD(MINUTE, 45*(ISNULL(OrderInTournament,1)-1), @StartTime)
                            WHERE TournamentId = @TournamentId",
                        new
                        {
                            auditableTournament.TournamentLocation?.MatchLocationId,
                            PlayerType = auditableTournament.PlayerType.ToString(),
                            auditableTournament.PlayersPerTeam,
                            StartTime = TimeZoneInfo.ConvertTimeToUtc(auditableTournament.StartTime.DateTime, TimeZoneInfo.FindSystemTimeZoneById(UkTimeZone())),
                            auditableTournament.TournamentId
                        },
                        transaction).ConfigureAwait(false);

                    // Update any transient teams with the amended tournament details
                    var transientTeams = await connection.QueryAsync<Team>($@"SELECT t.TeamId, t.TeamRoute FROM {Tables.TournamentTeam} tt
                                INNER JOIN {Tables.Team} t ON tt.TeamId = t.TeamId                               
                                WHERE tt.TournamentId = @TournamentId AND t.TeamType = '{TeamType.Transient.ToString()}'",
                            new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);

                    if (transientTeams.Any())
                    {
                        var transientTeamIds = transientTeams.Select(x => x.TeamId).OfType<Guid>();

                        await connection.ExecuteAsync($@"UPDATE {Tables.Team} SET
                                PlayerType = @PlayerType,
                                TeamRoute = CONCAT(@TournamentRoute, SUBSTRING(TeamRoute, {tournament.TournamentRoute.Length + 1}, LEN(TeamRoute)-{tournament.TournamentRoute.Length})) 
                                WHERE TeamId IN @transientTeamIds",
                            new
                            {
                                auditableTournament.PlayerType,
                                auditableTournament.TournamentRoute,
                                transientTeamIds
                            }, transaction).ConfigureAwait(false);

                        await connection.ExecuteAsync($@"UPDATE {Tables.TeamVersion} SET
                                UntilDate = @UntilDate
                                WHERE TeamId IN @transientTeamIds",
                            new
                            {
                                UntilDate = new DateTime(tournament.StartTime.Year, 12, 31).ToUniversalTime(),
                                transientTeamIds
                            }, transaction).ConfigureAwait(false);

                        await connection.ExecuteAsync($"DELETE FROM {Tables.TeamMatchLocation} WHERE TeamId IN @transientTeamIds", new { transientTeamIds }, transaction).ConfigureAwait(false);
                        if (auditableTournament.TournamentLocation != null)
                        {
                            foreach (var transientTeam in transientTeams)
                            {
                                await connection.ExecuteAsync($@"INSERT INTO {Tables.TeamMatchLocation} 
                                        (TeamMatchLocationId, TeamId, MatchLocationId, FromDate) 
                                        VALUES (@TeamMatchLocationId, @TeamId, @MatchLocationId, @FromDate)",
                                    new
                                    {
                                        TeamMatchLocationId = Guid.NewGuid(),
                                        transientTeam.TeamId,
                                        auditableTournament.TournamentLocation.MatchLocationId,
                                        FromDate = TimeZoneInfo.ConvertTimeToUtc(auditableTournament.StartTime.DateTime, TimeZoneInfo.FindSystemTimeZoneById(UkTimeZone())).Date
                                    }, transaction).ConfigureAwait(false);
                            }
                        }
                    }

                    if (tournament.TournamentRoute != auditableTournament.TournamentRoute)
                    {
                        await _redirectsRepository.InsertRedirect(tournament.TournamentRoute, auditableTournament.TournamentRoute, null, transaction).ConfigureAwait(false);
                        foreach (var transientTeam in transientTeams)
                        {
                            await _redirectsRepository.InsertRedirect(transientTeam.TeamRoute, auditableTournament.TournamentRoute + transientTeam.TeamRoute.Substring(tournament.TournamentRoute.Length), null, transaction).ConfigureAwait(false);
                        }
                    }

                    var redacted = _stoolballEntityCopier.CreateRedactedCopy(auditableTournament);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableTournament.EntityUri,
                        State = JsonConvert.SerializeObject(auditableTournament),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    },
                    transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(UpdateTournament));
                }
            }

            return auditableTournament;
        }

        private static async Task UpdateOverSets(Tournament auditableTournament, IDbTransaction transaction)
        {
            if (auditableTournament.DefaultOverSets.Any())
            {
                await transaction.Connection.ExecuteAsync($@"DELETE FROM {Tables.OverSet} WHERE TournamentId = @TournamentId AND OverSetNumber NOT IN (@OverSetNumbers)",
                    new
                    {
                        auditableTournament.TournamentId,
                        OverSetNumbers = auditableTournament.DefaultOverSets.Select(x => x.OverSetNumber)
                    },
                    transaction);

                var existingDefaultOversets = await transaction.Connection.QueryAsync<OverSet>(
                    $"SELECT OverSetId, OverSetNumber FROM {Tables.OverSet} WHERE TournamentId = @TournamentId",
                    new { auditableTournament.TournamentId }, transaction
                    );

                foreach (var overSet in auditableTournament.DefaultOverSets)
                {
                    var existingOverset = existingDefaultOversets.SingleOrDefault(x => x.OverSetNumber == overSet.OverSetNumber);
                    if (existingOverset != null)
                    {
                        await transaction.Connection.ExecuteAsync(
                            $@"UPDATE {Tables.OverSet} SET
                                      Overs = @Overs,
                                      BallsPerOver = @BallsPerOver
                                      WHERE OverSetId = @OverSetId",
                            new
                            {
                                overSet.Overs,
                                overSet.BallsPerOver,
                                existingOverset.OverSetId
                            }, transaction
                        );
                    }
                    else
                    {
                        overSet.OverSetId = Guid.NewGuid();
                        await transaction.Connection.ExecuteAsync(
                            $@"INSERT INTO {Tables.OverSet} (OverSetId, TournamentId, OverSetNumber, Overs, BallsPerOver) 
                                       VALUES (@OverSetId, @TournamentId, @OverSetNumber, @Overs, @BallsPerOver)",
                            new
                            {
                                overSet.OverSetId,
                                auditableTournament.TournamentId,
                                overSet.OverSetNumber,
                                overSet.Overs,
                                overSet.BallsPerOver
                            },
                            transaction);
                    }
                }

                // Replace overset for matches only if the tournament is in the future
                if (auditableTournament.StartTime > DateTimeOffset.UtcNow)
                {
                    var matchInningsIds = await transaction.Connection.QueryAsync<Guid>($"SELECT MatchInningsId FROM {Tables.MatchInnings} mi INNER JOIN {Tables.Match} m ON mi.MatchId = m.MatchId WHERE m.TournamentId = @TournamentId", new { auditableTournament.TournamentId }, transaction);
                    await transaction.Connection.ExecuteAsync($@"DELETE FROM {Tables.OverSet} WHERE MatchInningsId IN @matchInningsIds AND OverSetNumber NOT IN (@OverSetNumbers)",
                    new
                    {
                        matchInningsIds,
                        OverSetNumbers = auditableTournament.DefaultOverSets.Select(x => x.OverSetNumber)
                    },
                    transaction);

                    var existingMatchOversets = await transaction.Connection.QueryAsync<OverSetDto>(
                        $"SELECT OverSetId, MatchInningsId, OverSetNumber FROM {Tables.OverSet} WHERE MatchInningsId IN @matchInningsIds",
                        new { matchInningsIds }, transaction
                    );

                    foreach (var overSet in auditableTournament.DefaultOverSets)
                    {
                        foreach (var matchInningsId in matchInningsIds)
                        {
                            var existingOverset = existingMatchOversets.SingleOrDefault(x => x.MatchInningsId == matchInningsId && x.OverSetNumber == overSet.OverSetNumber);

                            if (existingOverset != null)
                            {
                                await transaction.Connection.ExecuteAsync(
                                    $@"UPDATE {Tables.OverSet} SET
                                                  Overs = @Overs,
                                                  BallsPerOver = @BallsPerOver
                                                  WHERE OverSetId = @OverSetId",
                                    new
                                    {
                                        overSet.Overs,
                                        overSet.BallsPerOver,
                                        existingOverset.OverSetId
                                    }, transaction
                                );
                            }
                            else
                            {
                                overSet.OverSetId = Guid.NewGuid();
                                await transaction.Connection.ExecuteAsync(
                                    $@"INSERT INTO {Tables.OverSet} (OverSetId, MatchInningsId, OverSetNumber, Overs, BallsPerOver) 
                                       VALUES (@OverSetId, @matchInningsId, @OverSetNumber, @Overs, @BallsPerOver)",
                                    new
                                    {
                                        overSet.OverSetId,
                                        matchInningsId,
                                        overSet.OverSetNumber,
                                        overSet.Overs,
                                        overSet.BallsPerOver
                                    },
                                    transaction);
                            }
                        }

                    }
                }
            }
            else
            {
                await transaction.Connection.ExecuteAsync($@"DELETE FROM {Tables.OverSet} WHERE TournamentId = @TournamentId", new { auditableTournament.TournamentId }, transaction);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Dapper uses it.")]
        private class TournamentTeamResult
        {
            public Guid? TournamentTeamId { get; set; }
            public Guid? TeamId { get; set; }
            public TeamType TeamType { get; set; }
        }

        /// <summary>
        /// Updates teams in a stoolball tournament
        /// </summary>
        public async Task<Tournament> UpdateTeams(Tournament tournament, Guid memberKey, string memberUsername, string memberName)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            if (string.IsNullOrWhiteSpace(memberUsername))
            {
                throw new ArgumentException($"'{nameof(memberUsername)}' cannot be null or whitespace", nameof(memberUsername));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableTournament = _stoolballEntityCopier.CreateAuditableCopy(tournament);
            if (auditableTournament.MaximumTeamsInTournament.HasValue)
            {
                auditableTournament.SpacesInTournament = auditableTournament.MaximumTeamsInTournament - auditableTournament.Teams.Count >= 0 ? auditableTournament.MaximumTeamsInTournament - auditableTournament.Teams.Count : 0;
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await connection.ExecuteAsync($@"UPDATE {Tables.Tournament} SET
						    MaximumTeamsInTournament = @MaximumTeamsInTournament,
                            SpacesInTournament = @SpacesInTournament
                            WHERE TournamentId = @TournamentId",
                    new
                    {
                        auditableTournament.MaximumTeamsInTournament,
                        auditableTournament.SpacesInTournament,
                        auditableTournament.TournamentId
                    }, transaction).ConfigureAwait(false);

                    var currentTeams = await connection.QueryAsync<TournamentTeamResult>(
                            $@"SELECT tt.TournamentTeamId, t.TeamId, t.TeamType 
                                   FROM {Tables.TournamentTeam} tt INNER JOIN {Tables.Team} t ON tt.TeamId = t.TeamId
                                   WHERE tt.TournamentId = @TournamentId", new { auditableTournament.TournamentId }, transaction
                        ).ConfigureAwait(false);

                    foreach (var team in auditableTournament.Teams)
                    {
                        var currentTeam = currentTeams.SingleOrDefault(x => x.TeamId == team.Team.TeamId);

                        // Team added
                        if (currentTeam == null)
                        {
                            var existingTeamId = await connection.ExecuteScalarAsync<Guid?>($"SELECT TeamId FROM {Tables.Team} WHERE TeamId = @TeamId", new { team.Team.TeamId }, transaction).ConfigureAwait(false);

                            if (existingTeamId == null)
                            {
                                team.Team.TeamType = TeamType.Transient;
                                team.Team.TeamRoute = auditableTournament.TournamentRoute;
                                team.Team.PlayerType = auditableTournament.PlayerType;
                                team.Team.UntilYear = auditableTournament.StartTime.Year;
                                if (auditableTournament.TournamentLocation != null) { team.Team.MatchLocations.Add(auditableTournament.TournamentLocation); }

                                team.Team = await _teamRepository.CreateTeam(team.Team, transaction, memberUsername).ConfigureAwait(false);

                                var serialisedTeam = JsonConvert.SerializeObject(team);
                                await _auditRepository.CreateAudit(new AuditRecord
                                {
                                    Action = AuditAction.Create,
                                    MemberKey = memberKey,
                                    ActorName = memberName,
                                    EntityUri = team.Team.EntityUri,
                                    State = serialisedTeam,
                                    RedactedState = serialisedTeam,
                                    AuditDate = DateTime.UtcNow
                                }, transaction).ConfigureAwait(false);

                                _logger.Info(LoggingTemplates.Created, team, memberName, memberKey, GetType(), nameof(UpdateTeams));
                            }

                            await connection.ExecuteAsync($@"INSERT INTO {Tables.TournamentTeam} 
                                    (TournamentTeamId, TournamentId, TeamId, TeamRole) 
                                    VALUES (@TournamentTeamId, @TournamentId, @TeamId, @TeamRole)",
                                    new
                                    {
                                        TournamentTeamId = Guid.NewGuid(),
                                        auditableTournament.TournamentId,
                                        team.Team.TeamId,
                                        TeamRole = TournamentTeamRole.Confirmed.ToString()
                                    },
                                    transaction).ConfigureAwait(false);
                        }
                    }

                    // Team removed?
                    var tournamentTeamIds = auditableTournament.Teams.Select(x => x.Team.TeamId);
                    var teamsToRemove = currentTeams.Where(x => !tournamentTeamIds.Contains(x.TeamId));
                    foreach (var team in teamsToRemove)
                    {
                        await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET OppositionTeamId = NULL, OppositionTeamName = NULL WHERE OppositionTeamId = @TeamId AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInMatchStatistics} WHERE TeamId = @TeamId AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET BowledByPlayerIdentityId = NULL WHERE BowledByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET CaughtByPlayerIdentityId = NULL WHERE CaughtByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET RunOutByPlayerIdentityId = NULL WHERE RunOutByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND TournamentId = @TournamentId", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"UPDATE {Tables.PlayerInnings} SET DismissedByPlayerIdentityId = NULL WHERE DismissedByPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"UPDATE {Tables.PlayerInnings} SET BowlerPlayerIdentityId = NULL WHERE BowlerPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInnings} WHERE BatterPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.Over} WHERE BowlerPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.BowlingFigures} WHERE BowlerPlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.AwardedTo} WHERE PlayerIdentityId IN (SELECT PlayerIdentityId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId) AND (TournamentId = @TournamentId OR MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"UPDATE {Tables.MatchInnings} SET BattingMatchTeamId = NULL WHERE BattingMatchTeamId IN (SELECT MatchTeamId FROM {Tables.MatchTeam} WHERE TeamId = @TeamId) AND MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"UPDATE {Tables.MatchInnings} SET BowlingMatchTeamId = NULL WHERE BowlingMatchTeamId IN (SELECT MatchTeamId FROM {Tables.MatchTeam} WHERE TeamId = @TeamId) AND MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.MatchTeam} WHERE TeamId = @TeamId AND MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { team.TeamId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentTeam} WHERE TournamentTeamId = @TournamentTeamId", new { team.TournamentTeamId }, transaction).ConfigureAwait(false);

                        if (team.TeamType == TeamType.Transient)
                        {
                            var playerIds = await connection.QueryAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId", new { team.TeamId }, transaction).ConfigureAwait(false);
                            await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerIdentity} WHERE TeamId = @TeamId", new { team.TeamId }, transaction).ConfigureAwait(false);
                            await connection.ExecuteAsync($"DELETE FROM {Tables.Player} WHERE PlayerId IN @playerIds", new { playerIds }, transaction).ConfigureAwait(false);
                            await connection.ExecuteAsync($"DELETE FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId", new { team.TeamId }, transaction).ConfigureAwait(false);
                            await connection.ExecuteAsync($"DELETE FROM {Tables.TeamVersion} WHERE TeamId = @TeamId", new { team.TeamId }, transaction).ConfigureAwait(false);
                            await connection.ExecuteAsync($"DELETE FROM {Tables.Team} WHERE TeamId = @TeamId", new { team.TeamId }, transaction).ConfigureAwait(false);
                        }
                    }

                    var redacted = _stoolballEntityCopier.CreateRedactedCopy(auditableTournament);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableTournament.EntityUri,
                        State = JsonConvert.SerializeObject(auditableTournament),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    },
                    transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(UpdateTeams));
                }
            }


            return auditableTournament;
        }

        /// <summary>
        /// Updates the seasons a stoolball tournament is listed in
        /// </summary>
        public async Task<Tournament> UpdateSeasons(Tournament tournament, Guid memberKey, string memberUsername, string memberName)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            if (string.IsNullOrWhiteSpace(memberUsername))
            {
                throw new ArgumentException($"'{nameof(memberUsername)}' cannot be null or whitespace", nameof(memberUsername));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableTournament = _stoolballEntityCopier.CreateAuditableCopy(tournament);
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var currentSeasons = await connection.QueryAsync<(Guid tournamentSeasonId, Guid seasonId)>(
                            $@"SELECT TournamentSeasonId, SeasonId
                                   FROM {Tables.TournamentSeason} 
                                   WHERE TournamentId = @TournamentId", new { auditableTournament.TournamentId }, transaction
                        ).ConfigureAwait(false);

                    foreach (var season in auditableTournament.Seasons)
                    {
                        // Season added
                        if (!currentSeasons.Any(x => x.seasonId == season.SeasonId))
                        {
                            await connection.ExecuteAsync($@"INSERT INTO {Tables.TournamentSeason} 
                                    (TournamentSeasonId, TournamentId, SeasonId) 
                                    VALUES (@TournamentSeasonId, @TournamentId, @SeasonId)",
                                    new
                                    {
                                        TournamentSeasonId = Guid.NewGuid(),
                                        auditableTournament.TournamentId,
                                        season.SeasonId
                                    },
                                    transaction).ConfigureAwait(false);
                        }
                    }

                    // Season removed?
                    var tournamentSeasonIds = auditableTournament.Seasons.Select(x => x.SeasonId);
                    var seasonsToRemove = currentSeasons.Where(x => !tournamentSeasonIds.Contains(x.seasonId));
                    foreach (var season in seasonsToRemove)
                    {
                        await connection.ExecuteAsync($"UPDATE {Tables.PlayerInMatchStatistics} SET SeasonId = NULL WHERE SeasonId = @SeasonId AND TournamentId = @TournamentId", new { season.seasonId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"UPDATE {Tables.Match} SET SeasonId = NULL WHERE SeasonId = @SeasonId AND TournamentId = @TournamentId", new { season.seasonId, auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentSeason} WHERE TournamentSeasonId = @TournamentSeasonId", new { season.tournamentSeasonId }, transaction).ConfigureAwait(false);
                    }

                    var redacted = _stoolballEntityCopier.CreateRedactedCopy(auditableTournament);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableTournament.EntityUri,
                        State = JsonConvert.SerializeObject(auditableTournament),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    },
                    transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(UpdateSeasons));
                }
            }

            return auditableTournament;
        }

        public async Task<Tournament> UpdateMatches(Tournament tournament, Guid memberKey, string memberUsername, string memberName)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            if (string.IsNullOrWhiteSpace(memberUsername))
            {
                throw new ArgumentException($"'{nameof(memberUsername)}' cannot be null or whitespace", nameof(memberUsername));
            }

            if (string.IsNullOrWhiteSpace(memberName))
            {
                throw new ArgumentNullException(nameof(memberName));
            }

            var auditableTournament = _stoolballEntityCopier.CreateAuditableCopy(tournament);
            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    var currentMatchesInTournament = await connection.QueryAsync<Guid>($"SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId", new { tournament.TournamentId }, transaction).ConfigureAwait(false);
                    var deletedMatches = currentMatchesInTournament.Where(x => !tournament.Matches.Where(m => m.MatchId.HasValue).Select(m => m.MatchId!.Value).Contains(x));
                    if (deletedMatches.Any())
                    {
                        foreach (var match in deletedMatches)
                        {
                            await _matchRepository.DeleteMatch(new Match { MatchId = match }, memberKey, memberName, transaction).ConfigureAwait(false);
                        }
                    }

                    for (var i = 0; i < tournament.Matches.Count; i++)
                    {
                        if (tournament.Matches[i].MatchId.HasValue)
                        {
                            _ = await connection.ExecuteAsync($"UPDATE {Tables.Match} SET OrderInTournament = @OrderInTournament WHERE MatchId = @MatchId",
                                new
                                {
                                    OrderInTournament = i + 1,
                                    MatchId = tournament.Matches[i].MatchId!.Value
                                },
                                transaction).ConfigureAwait(false);
                        }
                        else
                        {
                            var match = new Match
                            {
                                MatchType = MatchType.GroupMatch,
                                Tournament = tournament,
                                PlayerType = tournament.PlayerType,
                                PlayersPerTeam = tournament.PlayersPerTeam,
                                MatchLocation = tournament.TournamentLocation,
                                OrderInTournament = i + 1,
                                StartTime = tournament.StartTime.AddMinutes(45 * i),
                                StartTimeIsKnown = false,
                                Teams = tournament.Matches[i].Teams.Select(x => new TeamInMatch { Team = tournament.Teams.Single(t => t.TournamentTeamId == x.TournamentTeamId).Team }).ToList()
                            };
                            if (match.Teams.Count > 0) { match.Teams[0].TeamRole = TeamRole.Home; }
                            if (match.Teams.Count > 1) { match.Teams[1].TeamRole = TeamRole.Away; }

                            _ = await _matchRepository.CreateMatch(match, memberKey, memberName, transaction).ConfigureAwait(false);
                        }
                    }

                    var redacted = _stoolballEntityCopier.CreateRedactedCopy(auditableTournament);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Update,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableTournament.EntityUri,
                        State = JsonConvert.SerializeObject(auditableTournament),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    },
                    transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Updated, redacted, memberName, memberKey, GetType(), nameof(UpdateMatches));
                }
            }

            return auditableTournament;
        }

        /// <summary>
        /// Deletes a stoolball tournament
        /// </summary>
        public async Task DeleteTournament(Tournament tournament, Guid memberKey, string memberName)
        {
            if (tournament is null)
            {
                throw new ArgumentNullException(nameof(tournament));
            }

            var auditableTournament = _stoolballEntityCopier.CreateAuditableCopy(tournament);

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    // Delete all matches and statistics in the tournament
                    await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInMatchStatistics} WHERE TournamentId = @TournamentId", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Over} WHERE MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.BowlingFigures} WHERE MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerInnings} WHERE MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.OverSet} WHERE TournamentId = @TournamentId OR MatchInningsId IN (SELECT MatchInningsId FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId))", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.MatchInnings} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.MatchTeam} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Comment} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.AwardedTo} WHERE TournamentId = @TournamentId OR MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Match} WHERE MatchId IN (SELECT MatchId FROM {Tables.Match} WHERE TournamentId = @TournamentId)", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);

                    // Remove teams from the tournament. Delete the transient teams. (Player performances for transient teams should already be removed above.)
                    var transientTeamIds = await connection.QueryAsync<Guid>($@"SELECT t.TeamId
                            FROM { Tables.TournamentTeam} tt
                            INNER JOIN { Tables.Team} t ON tt.TeamId = t.TeamId
                            WHERE t.TeamType = '{TeamType.Transient.ToString()}' AND tt.TournamentId = @TournamentId"
                        , new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);

                    await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentTeam} WHERE TournamentId = @TournamentId", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);

                    if (transientTeamIds.Any())
                    {

                        var playerIds = await connection.QueryAsync<Guid>($"SELECT PlayerId FROM {Tables.PlayerIdentity} WHERE TeamId IN @transientTeamIds", new { transientTeamIds }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.PlayerIdentity} WHERE TeamId IN @transientTeamIds", new { transientTeamIds }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($"DELETE FROM {Tables.Player} WHERE PlayerId IN @playerIds", new { playerIds }, transaction).ConfigureAwait(false);

                        await connection.ExecuteAsync($@"DELETE FROM {Tables.TeamMatchLocation} WHERE TeamId IN @transientTeamIds", new { transientTeamIds }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($@"DELETE FROM {Tables.TeamVersion} WHERE TeamId IN @transientTeamIds", new { transientTeamIds }, transaction).ConfigureAwait(false);
                        await connection.ExecuteAsync($@"DELETE FROM {Tables.Team} WHERE TeamId IN @transientTeamIds", new { transientTeamIds }, transaction).ConfigureAwait(false);
                    }

                    // Delete other related data and the tournament itself
                    await connection.ExecuteAsync($"DELETE FROM {Tables.TournamentSeason} WHERE TournamentId = @TournamentId", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Comment} WHERE TournamentId = @TournamentId", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);
                    await connection.ExecuteAsync($"DELETE FROM {Tables.Tournament} WHERE TournamentId = @TournamentId", new { auditableTournament.TournamentId }, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix(auditableTournament.TournamentRoute, transaction).ConfigureAwait(false);

                    var redacted = _stoolballEntityCopier.CreateRedactedCopy(auditableTournament);
                    await _auditRepository.CreateAudit(new AuditRecord
                    {
                        Action = AuditAction.Delete,
                        MemberKey = memberKey,
                        ActorName = memberName,
                        EntityUri = auditableTournament.EntityUri,
                        State = JsonConvert.SerializeObject(auditableTournament),
                        RedactedState = JsonConvert.SerializeObject(redacted),
                        AuditDate = DateTime.UtcNow
                    },
                    transaction).ConfigureAwait(false);

                    transaction.Commit();

                    _logger.Info(LoggingTemplates.Deleted, redacted, memberName, memberKey, GetType(), nameof(DeleteTournament));
                }
            }
        }

    }
}
