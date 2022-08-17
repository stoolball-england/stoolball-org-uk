using System;
using System.Data;
using System.Globalization;
using Dapper;
using Stoolball.Awards;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Testing;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    public class SqlServerIntegrationTestsRepository
    {
        private readonly IDbConnection _connection;
        private readonly IPlayerInMatchStatisticsBuilder _playerInMatchStatisticsBuilder;

        public SqlServerIntegrationTestsRepository(IDbConnection connection, IPlayerInMatchStatisticsBuilder playerInMatchStatisticsBuilder)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _playerInMatchStatisticsBuilder = playerInMatchStatisticsBuilder ?? throw new ArgumentNullException(nameof(playerInMatchStatisticsBuilder));
        }

        public void CreateTestData(TestData data)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            foreach (var member in data.Members)
            {
                CreateMember(member);
            }

            foreach (var player in data.Players)
            {
                CreatePlayer(player);
            }

            foreach (var school in data.Schools)
            {
                CreateSchool(school);
            }

            foreach (var team in data.Teams)
            {
                if (team.Club != null)
                {
                    CreateClub(team.Club);
                }
                CreateTeam(team);
            }
            foreach (var playerIdentity in data.PlayerIdentities)
            {
                CreatePlayerIdentity(playerIdentity);
            }
            foreach (var location in data.MatchLocations)
            {
                CreateMatchLocation(location);
            }

            foreach (var team in data.Teams)
            {
                foreach (var location in team.MatchLocations)
                {
                    AddTeamToMatchLocation(team, location);
                }
            }

            foreach (var competition in data.Competitions)
            {
                CreateCompetition(competition);
            }
            foreach (var season in data.Seasons)
            {
                CreateSeason(season, season.Competition.CompetitionId.Value);
                foreach (var teamInSeason in season.Teams)
                {
                    AddTeamToSeason(teamInSeason);
                }
            }
            foreach (var tournament in data.Tournaments)
            {
                CreateTournament(tournament);
                foreach (var teamInTournament in tournament.Teams)
                {
                    AddTeamToTournament(teamInTournament, tournament);
                }
                foreach (var season in tournament.Seasons)
                {
                    AddTournamentToSeason(tournament, season);
                }
            }

            foreach (var match in data.Matches)
            {
                CreateMatch(match);

                var statisticsRecords = _playerInMatchStatisticsBuilder.BuildStatisticsForMatch(match);
                foreach (var record in statisticsRecords)
                {
                    CreatePlayerInMatchStatisticsRecord(record);
                }
            }
        }

        private void CreateSchool(Stoolball.Schools.School school)
        {
            _connection.Execute($@"INSERT INTO {Tables.School} 
                    (SchoolId, Website, Twitter, Facebook, Instagram, YouTube, MemberGroupKey, MemberGroupName, SchoolRoute)
                    VALUES
                    (@SchoolId, @Website, @Twitter, @Facebook, @Instagram, @YouTube, @MemberGroupKey, @MemberGroupName, @SchoolRoute)",
                 new
                 {
                     school.SchoolId,
                     school.Website,
                     school.Twitter,
                     school.Facebook,
                     school.Instagram,
                     school.YouTube,
                     school.MemberGroupKey,
                     school.MemberGroupName,
                     school.SchoolRoute
                 });

            _connection.Execute($@"INSERT INTO {Tables.SchoolVersion}
                    (SchoolVersionId, SchoolId, SchoolName, ComparableName, FromDate, UntilDate)
                    VALUES
                    (@SchoolVersionId, @SchoolId, @SchoolName, @ComparableName, @FromDate, @UntilDate)",
                    new
                    {
                        SchoolVersionId = Guid.NewGuid(),
                        school.SchoolId,
                        school.SchoolName,
                        ComparableName = school.ComparableName(),
                        FromDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                        UntilDate = school.UntilYear.HasValue ? new DateTimeOffset(school.UntilYear.Value, 12, 31, 23, 59, 59, TimeSpan.Zero) : (DateTimeOffset?)null
                    });
        }

        public void CreateTournament(Tournament tournament)
        {
            _connection.Execute($@"INSERT INTO {Tables.Tournament} 
                    (TournamentId, TournamentName, MatchLocationId, StartTime, StartTimeIsKnown, PlayerType, PlayersPerTeam, QualificationType, MaximumTeamsInTournament, SpacesInTournament, TournamentNotes, TournamentRoute, MemberKey)
                    VALUES
                    (@TournamentId, @TournamentName, @MatchLocationId, @StartTime, @StartTimeIsKnown, @PlayerType, @PlayersPerTeam, @QualificationType, @MaximumTeamsInTournament, @SpacesInTournament, @TournamentNotes, @TournamentRoute, @MemberKey)",
                   new
                   {
                       tournament.TournamentId,
                       tournament.TournamentName,
                       MatchLocationId = tournament.TournamentLocation?.MatchLocationId,
                       StartTime = TimeZoneInfo.ConvertTimeToUtc(tournament.StartTime.DateTime, TimeZoneInfo.FindSystemTimeZoneById(UkTimeZone())),
                       tournament.StartTimeIsKnown,
                       PlayerType = tournament.PlayerType.ToString(),
                       tournament.PlayersPerTeam,
                       tournament.QualificationType,
                       tournament.MaximumTeamsInTournament,
                       tournament.SpacesInTournament,
                       tournament.TournamentNotes,
                       tournament.TournamentRoute,
                       tournament.MemberKey
                   });

            foreach (var overSet in tournament.DefaultOverSets)
            {
                CreateOverSet(overSet, null, null, tournament.TournamentId);
            }

            foreach (var comment in tournament.Comments)
            {
                _connection.Execute($@"INSERT INTO {Tables.Comment} 
                                      (CommentId, TournamentId, MemberKey, CommentDate, Comment) 
                                       VALUES 
                                      (@CommentId, @TournamentId, @MemberKey, @CommentDate, @Comment)",
                    new
                    {
                        comment.CommentId,
                        tournament.TournamentId,
                        comment.MemberKey,
                        CommentDate = comment.CommentDate.UtcDateTime,
                        comment.Comment
                    });
            }
        }

        public void CreateClub(Club club)
        {
            _connection.Execute($@"INSERT INTO {Tables.Club} 
                    (ClubId, MemberGroupKey, MemberGroupName, ClubRoute)
                    VALUES
                    (@ClubId, @MemberGroupKey, @MemberGroupName, @ClubRoute)",
                   new
                   {
                       club.ClubId,
                       club.MemberGroupKey,
                       club.MemberGroupName,
                       club.ClubRoute
                   });

            _connection.Execute($@"INSERT INTO {Tables.ClubVersion}
                    (ClubVersionId, ClubId, ClubName, ComparableName, FromDate, UntilDate)
                    VALUES
                    (@ClubVersionId, @ClubId, @ClubName, @ComparableName, @FromDate, @UntilDate)",
                    new
                    {
                        ClubVersionId = Guid.NewGuid(),
                        club.ClubId,
                        club.ClubName,
                        ComparableName = club.ComparableName(),
                        FromDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                        UntilDate = club.UntilDate?.UtcDateTime
                    });
        }

        public void CreateMatch(Match match)
        {
            _connection.Execute($@"INSERT INTO {Tables.Match} 
                    (MatchId, MatchName, UpdateMatchNameAutomatically, MatchType, PlayerType, StartTime, StartTimeIsKnown, MatchRoute, MatchLocationId, PlayersPerTeam, 
                     EnableBonusOrPenaltyRuns, LastPlayerBatsOn, InningsOrderIsKnown, MatchResultType, MatchNotes, SeasonId, TournamentId, OrderInTournament, MemberKey)
                    VALUES
                    (@MatchId, @MatchName, @UpdateMatchNameAutomatically, @MatchType, @PlayerType, @StartTime, @StartTimeIsKnown, @MatchRoute, @MatchLocationId, @PlayersPerTeam, 
                     @EnableBonusOrPenaltyRuns, @LastPlayerBatsOn, @InningsOrderIsKnown, @MatchResultType, @MatchNotes, @SeasonId, @TournamentId, @OrderInTournament, @MemberKey)", new
            {

                match.MatchId,
                match.MatchName,
                match.UpdateMatchNameAutomatically,
                MatchType = match.MatchType.ToString(),
                PlayerType = match.PlayerType.ToString(),
                StartTime = TimeZoneInfo.ConvertTimeToUtc(match.StartTime.DateTime, TimeZoneInfo.FindSystemTimeZoneById(UkTimeZone())),
                match.StartTimeIsKnown,
                match.MatchRoute,
                match.MatchLocation?.MatchLocationId,
                match.PlayersPerTeam,
                match.EnableBonusOrPenaltyRuns,
                match.LastPlayerBatsOn,
                match.InningsOrderIsKnown,
                MatchResultType = match.MatchResultType?.ToString(),
                match.MatchNotes,
                match.Season?.SeasonId,
                match.Tournament?.TournamentId,
                match.OrderInTournament,
                match.MemberKey
            });

            foreach (var teamInMatch in match.Teams)
            {
                _connection.Execute($@"INSERT INTO {Tables.MatchTeam} 
                    (MatchTeamId, TeamId, PlayingAsTeamName, MatchId, TeamRole, WonToss, WinnerOfMatchId)
                    VALUES
                    (@MatchTeamId, @TeamId, @PlayingAsTeamName, @MatchId, @TeamRole, @WonToss, @WinnerOfMatchId)",
                   new
                   {
                       teamInMatch.MatchTeamId,
                       teamInMatch.Team.TeamId,
                       teamInMatch.PlayingAsTeamName,
                       match.MatchId,
                       TeamRole = teamInMatch.TeamRole.ToString(),
                       teamInMatch.WonToss,
                       WinnerOfMatchId = (Guid?)null
                   });
            }

            foreach (var innings in match.MatchInnings)
            {
                _connection.Execute($@"INSERT INTO {Tables.MatchInnings} 
                    (MatchInningsId, MatchId, InningsOrderInMatch, BattingMatchTeamId, BowlingMatchTeamId, Byes, Wides, NoBalls, BonusOrPenaltyRuns, Runs, Wickets)
                    VALUES
                    (@MatchInningsId, @MatchId, @InningsOrderInMatch, @BattingMatchTeamId, @BowlingMatchTeamId, @Byes, @Wides, @NoBalls, @BonusOrPenaltyRuns, @Runs, @Wickets)",
                    new
                    {
                        innings.MatchInningsId,
                        match.MatchId,
                        innings.InningsOrderInMatch,
                        innings.BattingMatchTeamId,
                        innings.BowlingMatchTeamId,
                        innings.Byes,
                        innings.Wides,
                        innings.NoBalls,
                        innings.BonusOrPenaltyRuns,
                        innings.Runs,
                        innings.Wickets
                    });
            }

            foreach (var award in match.Awards)
            {
                _connection.Execute($@"INSERT INTO {Tables.Award} 
                    (AwardId, AwardName, AwardScope)
                    VALUES
                    (@AwardId, @AwardName, @AwardScope)",
                  new
                  {
                      award.Award.AwardId,
                      award.Award.AwardName,
                      AwardScope = AwardScope.Match
                  });

                _connection.Execute($@"INSERT INTO {Tables.AwardedTo} 
                    (AwardedToId, AwardId, PlayerIdentityId, MatchId, Reason)
                    VALUES
                    (@AwardedToId, @AwardId, @PlayerIdentityId, @MatchId, @Reason)",
                  new
                  {
                      award.AwardedToId,
                      award.Award.AwardId,
                      award.PlayerIdentity.PlayerIdentityId,
                      match.MatchId,
                      award.Reason
                  });
            }

            foreach (var innings in match.MatchInnings)
            {
                foreach (var playerInnings in innings.PlayerInnings)
                {
                    _connection.Execute($@"INSERT INTO {Tables.PlayerInnings} 
                    (PlayerInningsId, MatchInningsId, BatterPlayerIdentityId, BattingPosition, DismissalType, DismissedByPlayerIdentityId, BowlerPlayerIdentityId, RunsScored, BallsFaced)
                    VALUES
                    (@PlayerInningsId, @MatchInningsId, @BatterPlayerIdentityId, @BattingPosition, @DismissalType, @DismissedByPlayerIdentityId, @BowlerPlayerIdentityId, @RunsScored, @BallsFaced)",
                  new
                  {
                      playerInnings.PlayerInningsId,
                      innings.MatchInningsId,
                      BatterPlayerIdentityId = playerInnings.Batter.PlayerIdentityId,
                      playerInnings.BattingPosition,
                      DismissalType = playerInnings.DismissalType.ToString(),
                      DismissedByPlayerIdentityId = playerInnings.DismissedBy?.PlayerIdentityId,
                      BowlerPlayerIdentityId = playerInnings.Bowler?.PlayerIdentityId,
                      playerInnings.RunsScored,
                      playerInnings.BallsFaced
                  });
                }

                foreach (var overSet in innings.OverSets)
                {
                    CreateOverSet(overSet, innings.MatchInningsId, null, null);
                }

                foreach (var overBowled in innings.OversBowled)
                {
                    _connection.Execute($@"INSERT INTO {Tables.Over} 
                    (OverId, MatchInningsId, OverSetId, BowlerPlayerIdentityId, OverNumber, BallsBowled, NoBalls, Wides, RunsConceded)
                    VALUES
                    (@OverId, @MatchInningsId, @OverSetId, @BowlerPlayerIdentityId, @OverNumber, @BallsBowled, @NoBalls, @Wides, @RunsConceded)",
                  new
                  {
                      overBowled.OverId,
                      innings.MatchInningsId,
                      overBowled.OverSet?.OverSetId,
                      BowlerPlayerIdentityId = overBowled.Bowler.PlayerIdentityId,
                      overBowled.OverNumber,
                      overBowled.BallsBowled,
                      overBowled.NoBalls,
                      overBowled.Wides,
                      overBowled.RunsConceded
                  });
                }

                var i = 1;
                foreach (var bowlingFigures in innings.BowlingFigures)
                {
                    _connection.Execute($@"INSERT INTO {Tables.BowlingFigures} 
                    (BowlingFiguresId, MatchInningsId, BowlingOrder, BowlerPlayerIdentityId, Overs, Maidens, RunsConceded, Wickets, IsFromOversBowled)
                    VALUES
                    (@BowlingFiguresId, @MatchInningsId, @BowlingOrder, @BowlerPlayerIdentityId, @Overs, @Maidens, @RunsConceded, @Wickets, @IsFromOversBowled)",
                  new
                  {
                      bowlingFigures.BowlingFiguresId,
                      innings.MatchInningsId,
                      BowlingOrder = 1,
                      BowlerPlayerIdentityId = bowlingFigures.Bowler.PlayerIdentityId,
                      bowlingFigures.Overs,
                      bowlingFigures.Maidens,
                      bowlingFigures.RunsConceded,
                      bowlingFigures.Wickets,
                      IsFromOversBowled = true
                  });
                    i++;
                }
            }

            foreach (var comment in match.Comments)
            {
                _connection.Execute($@"INSERT INTO {Tables.Comment} 
                                      (CommentId, MatchId, MemberKey, CommentDate, Comment) 
                                       VALUES 
                                      (@CommentId, @MatchId, @MemberKey, @CommentDate, @Comment)",
                    new
                    {
                        comment.CommentId,
                        match.MatchId,
                        comment.MemberKey,
                        CommentDate = comment.CommentDate.UtcDateTime,
                        comment.Comment
                    });
            }
        }

        internal void CreateAudit(AuditRecord audit)
        {
            _connection.Execute($@"INSERT INTO {Tables.Audit} 
                    (AuditId, MemberKey, ActorName, Action, EntityUri, State, RedactedState, AuditDate)
                    VALUES
                    (@AuditId, @MemberKey, @ActorName, @Action, @EntityUri, @State, @RedactedState, @AuditDate)",
            new
            {
                AuditId = Guid.NewGuid(),
                audit.MemberKey,
                audit.ActorName,
                Action = audit.Action.ToString(),
                EntityUri = audit.EntityUri.ToString(),
                audit.State,
                audit.RedactedState,
                audit.AuditDate
            });
        }

        public void AddTournamentToSeason(Tournament tournament, Season season)
        {
            _connection.Execute($@"INSERT INTO {Tables.TournamentSeason} 
                    (TournamentSeasonId, TournamentId, SeasonId)
                    VALUES
                    (@TournamentSeasonId, @TournamentId, @SeasonId)",
          new
          {
              TournamentSeasonId = Guid.NewGuid(),
              tournament.TournamentId,
              season.SeasonId
          });
        }

        public void AddTeamToTournament(TeamInTournament team, Tournament tournament)
        {
            _connection.Execute($@"INSERT INTO {Tables.TournamentTeam} 
                    (TournamentTeamId, TournamentId, TeamId, TeamRole, PlayingAsTeamName)
                    VALUES
                    (@TournamentTeamId, @TournamentId, @TeamId, @TeamRole, @PlayingAsTeamName)",
          new
          {
              TournamentTeamId = team.TournamentTeamId ?? Guid.NewGuid(),
              tournament.TournamentId,
              team.Team.TeamId,
              team.TeamRole,
              team.PlayingAsTeamName
          });
        }

        public void AddTeamToMatchLocation(Team team, MatchLocation matchLocation)
        {
            var teamMatchLocationId = _connection.QuerySingleOrDefault<Guid?>($"SELECT TeamMatchLocationId FROM {Tables.TeamMatchLocation} WHERE TeamId = @TeamId AND MatchLocationId = @MatchLocationId", new { team.TeamId, matchLocation.MatchLocationId });
            if (teamMatchLocationId.HasValue) { return; }

            _connection.Execute($@"INSERT INTO {Tables.TeamMatchLocation} 
                    (TeamMatchLocationId, MatchLocationId, TeamId, FromDate, UntilDate)
                    VALUES
                    (@TeamMatchLocationId, @MatchLocationId, @TeamId, @FromDate, @UntilDate)",
           new
           {
               TeamMatchLocationId = Guid.NewGuid(),
               matchLocation.MatchLocationId,
               team.TeamId,
               FromDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
               UntilDate = (DateTimeOffset?)null
           });
        }

        public void AddTeamToSeason(TeamInSeason teamInSeason)
        {
            _connection.Execute($@"INSERT INTO {Tables.SeasonTeam} 
                    (SeasonTeamId, SeasonId, TeamId, WithdrawnDate)
                    VALUES
                    (@SeasonTeamId, @SeasonId, @TeamId, @WithdrawnDate)",
            new
            {
                SeasonTeamId = Guid.NewGuid(),
                teamInSeason.Season.SeasonId,
                teamInSeason.Team.TeamId,
                WithdrawnDate = teamInSeason.WithdrawnDate.HasValue ? TimeZoneInfo.ConvertTimeToUtc(teamInSeason.WithdrawnDate.Value.Date, TimeZoneInfo.FindSystemTimeZoneById(Constants.UkTimeZone())) : (DateTime?)null
            });
        }

        public void CreateOverSet(OverSet overSet, Guid? matchInningsId, Guid? seasonId, Guid? tournamentId)
        {
            _connection.Execute($@"INSERT INTO {Tables.OverSet} 
                    (OverSetId, MatchInningsId, SeasonId, TournamentId, OverSetNumber, Overs, BallsPerOver)
                    VALUES
                    (@OverSetId, @MatchInningsId, @SeasonId, @TournamentId, @OverSetNumber, @Overs, @BallsPerOver)",
            new
            {
                overSet.OverSetId,
                MatchInningsId = matchInningsId,
                SeasonId = seasonId,
                TournamentId = tournamentId,
                overSet.OverSetNumber,
                overSet.Overs,
                overSet.BallsPerOver
            });
        }

        public void CreatePlayer(Player player)
        {
            _connection.Execute($@"INSERT INTO {Tables.Player} 
                    (PlayerId, PlayerRoute, MemberKey)
                    VALUES
                    (@PlayerId, @PlayerRoute, @MemberKey)",
                   player);
        }

        public void CreateMember((Guid memberId, string memberName) member)
        {
            var nodeId = _connection.QuerySingle<int>("SELECT MAX(Id) + 1 FROM umbracoNode");
            _connection.Execute("SET IDENTITY_INSERT umbracoNode ON");
            _connection.Execute($"INSERT INTO umbracoNode (id, uniqueid, parentid, level, path, sortOrder, text, nodeObjectType) VALUES (@nodeId, @memberId, @nodeId, 1, CONCAT('-1,', @nodeId), 0, @memberName, '9B5416FB-E72F-45A9-A07B-5A9A2709CE43')",
                                new { nodeId, member.memberId, member.memberName });
            _connection.Execute("SET IDENTITY_INSERT umbracoNode OFF");
            _connection.Execute("INSERT INTO umbracoContent (nodeId, contentTypeId) VALUES(@nodeId, (SELECT id FROM umbracoNode WHERE text = 'Member' AND nodeObjectType = '9B5416FB-E72F-45A9-A07B-5A9A2709CE43'))", new { nodeId });
            _connection.Execute("INSERT INTO cmsMember (nodeId, Email) VALUES(@nodeId, @email)", new { nodeId, email = member.memberName.ToLower(CultureInfo.CurrentCulture).Replace(" ", ".") + "@example.org" });
        }

        public void CreateUmbracoBaseRecords()
        {
            var memberNodeId = 1;

            _connection.Execute("SET IDENTITY_INSERT umbracoNode ON");
            _connection.Execute($"INSERT INTO umbracoNode (id, uniqueid, parentid, level, path, sortOrder, text, nodeObjectType) VALUES ({memberNodeId}, NEWID(), {memberNodeId}, 1, '-1,{memberNodeId}', 0, 'Member', '9B5416FB-E72F-45A9-A07B-5A9A2709CE43')");
            _connection.Execute("SET IDENTITY_INSERT umbracoNode OFF");
            _connection.Execute($"INSERT INTO cmsContentType(nodeId, alias, icon, thumbnail, isContainer, isElement, allowAtRoot, variations) VALUES ({memberNodeId}, 'Member', 'icon-user', 'icon-user', 0, 0, 0, 0)");
        }

        public void CreatePlayerIdentity(PlayerIdentity playerIdentity)
        {
            _connection.Execute($@"INSERT INTO {Tables.PlayerIdentity} 
                    (PlayerIdentityId, PlayerId, TeamId, PlayerIdentityName, ComparableName)
                    VALUES
                    (@PlayerIdentityId, @PlayerId, @TeamId, @PlayerIdentityName, @ComparableName)",
                   new
                   {
                       playerIdentity.PlayerIdentityId,
                       playerIdentity.Player.PlayerId,
                       playerIdentity.Team.TeamId,
                       playerIdentity.PlayerIdentityName,
                       ComparableName = playerIdentity.ComparableName()
                   });
        }

        public void CreateMatchLocation(MatchLocation matchLocation)
        {
            _connection.Execute($@"INSERT INTO {Tables.MatchLocation} 
                    (MatchLocationId, ComparableName, SecondaryAddressableObjectName, PrimaryAddressableObjectName, StreetDescription, Locality, Town, AdministrativeArea, Postcode,
                    Latitude, Longitude, GeoPrecision, MatchLocationNotes, MemberGroupKey, MemberGroupName, MatchLocationRoute)
                    VALUES
                    (@MatchLocationId, @ComparableName, @SecondaryAddressableObjectName, @PrimaryAddressableObjectName, @StreetDescription, @Locality, @Town, @AdministrativeArea, @Postcode,
                    @Latitude, @Longitude, @GeoPrecision, @MatchLocationNotes, @MemberGroupKey, @MemberGroupName, @MatchLocationRoute)",
                    new
                    {
                        matchLocation.MatchLocationId,
                        ComparableName = matchLocation.ComparableName(),
                        matchLocation.SecondaryAddressableObjectName,
                        matchLocation.PrimaryAddressableObjectName,
                        matchLocation.StreetDescription,
                        matchLocation.Locality,
                        matchLocation.Town,
                        matchLocation.AdministrativeArea,
                        matchLocation.Postcode,
                        matchLocation.Latitude,
                        matchLocation.Longitude,
                        matchLocation.GeoPrecision,
                        matchLocation.MatchLocationNotes,
                        matchLocation.MemberGroupKey,
                        matchLocation.MemberGroupName,
                        matchLocation.MatchLocationRoute
                    }
                    );

            foreach (var team in matchLocation.Teams)
            {
                CreateTeam(team);

                AddTeamToMatchLocation(team, matchLocation);
            }
        }

        public void CreateTeam(Team team)
        {
            var teamId = _connection.QuerySingleOrDefault<Guid?>($"SELECT TeamId FROM {Tables.Team} WHERE TeamId = @TeamId", new { team.TeamId });
            if (teamId.HasValue) { return; }

            _connection.Execute($@"INSERT INTO {Tables.Team} 
                    (TeamId, ClubId, ClubMark, SchoolId, TeamType, PlayerType, Introduction, AgeRangeLower, AgeRangeUpper, Website, Twitter, Facebook, Instagram, YouTube, 
                     PublicContactDetails, PrivateContactDetails, PlayingTimes, Cost, MemberGroupKey, MemberGroupName, TeamRoute)
                    VALUES
                    (@TeamId, @ClubId, @ClubMark, @SchoolId, @TeamType, @PlayerType, @Introduction, @AgeRangeLower, @AgeRangeUpper, @Website, @Twitter, @Facebook, @Instagram, @YouTube, 
                     @PublicContactDetails, @PrivateContactDetails, @PlayingTimes, @Cost, @MemberGroupKey, @MemberGroupName, @TeamRoute)",
                  new
                  {
                      team.TeamId,
                      team.Club?.ClubId,
                      team.ClubMark,
                      team.School?.SchoolId,
                      TeamType = team.TeamType.ToString(),
                      PlayerType = team.PlayerType.ToString(),
                      team.Introduction,
                      team.AgeRangeLower,
                      team.AgeRangeUpper,
                      team.Website,
                      team.Twitter,
                      team.Facebook,
                      team.Instagram,
                      team.YouTube,
                      team.PublicContactDetails,
                      team.PrivateContactDetails,
                      team.PlayingTimes,
                      team.Cost,
                      team.MemberGroupKey,
                      team.MemberGroupName,
                      team.TeamRoute
                  });

            _connection.Execute($@"INSERT INTO {Tables.TeamVersion}
                    (TeamVersionId, TeamId, TeamName, ComparableName, FromDate, UntilDate)
                    VALUES
                    (@TeamVersionId, @TeamId, @TeamName, @ComparableName, @FromDate, @UntilDate)",
                    new
                    {
                        TeamVersionId = Guid.NewGuid(),
                        team.TeamId,
                        team.TeamName,
                        ComparableName = team.ComparableName(),
                        FromDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                        UntilDate = team.UntilYear.HasValue ? new DateTimeOffset(team.UntilYear.Value, 12, 31, 23, 59, 59, TimeSpan.Zero) : (DateTimeOffset?)null
                    });
        }

        public void CreateCompetition(Competition competition)
        {
            _connection.Execute($@"INSERT INTO {Tables.Competition} 
                    (CompetitionId, Introduction, PublicContactDetails, PrivateContactDetails, Website, Twitter,
                    Facebook, Instagram, YouTube, PlayerType, MemberGroupKey, MemberGroupName, CompetitionRoute)
                    VALUES
                    (@CompetitionId, @Introduction, @PublicContactDetails, @PrivateContactDetails, @Website, @Twitter,
                    @Facebook, @Instagram, @YouTube, @PlayerType, @MemberGroupKey, @MemberGroupName, @CompetitionRoute)",
                    new
                    {
                        competition.CompetitionId,
                        competition.Introduction,
                        competition.PublicContactDetails,
                        competition.PrivateContactDetails,
                        competition.Website,
                        competition.Twitter,
                        competition.Facebook,
                        competition.Instagram,
                        competition.YouTube,
                        PlayerType = competition.PlayerType.ToString(),
                        competition.MemberGroupKey,
                        competition.MemberGroupName,
                        competition.CompetitionRoute
                    }
                    );

            _connection.Execute($@"INSERT INTO {Tables.CompetitionVersion}
                    (CompetitionVersionId, CompetitionId, CompetitionName, ComparableName, FromDate, UntilDate)
                    VALUES
                    (@CompetitionVersionId, @CompetitionId, @CompetitionName, @ComparableName, @FromDate, @UntilDate)",
                  new
                  {
                      CompetitionVersionId = Guid.NewGuid(),
                      competition.CompetitionId,
                      competition.CompetitionName,
                      ComparableName = competition.ComparableName(),
                      FromDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                      UntilDate = competition.UntilYear.HasValue ? new DateTimeOffset(competition.UntilYear.Value, 12, 31, 23, 59, 59, TimeSpan.Zero) : (DateTimeOffset?)null
                  });
        }

        public void CreateSeason(Season season, Guid competitionId)
        {
            _connection.Execute($@"INSERT INTO {Tables.Season} 
                    (SeasonId, CompetitionId, FromYear, UntilYear, Introduction, Results, PlayersPerTeam, EnableTournaments, EnableBonusOrPenaltyRuns,
                    ResultsTableType, EnableRunsScored, EnableRunsConceded, EnableLastPlayerBatsOn, SeasonRoute)
                    VALUES
                    (@SeasonId, @CompetitionId, @FromYear, @UntilYear, @Introduction, @Results, @PlayersPerTeam, @EnableTournaments, @EnableBonusOrPenaltyRuns,
                    @ResultsTableType, @EnableRunsScored, @EnableRunsConceded, @EnableLastPlayerBatsOn, @SeasonRoute)",
                    new
                    {
                        season.SeasonId,
                        CompetitionId = competitionId,
                        season.FromYear,
                        season.UntilYear,
                        season.Introduction,
                        season.Results,
                        season.PlayersPerTeam,
                        season.EnableTournaments,
                        season.EnableBonusOrPenaltyRuns,
                        ResultsTableType = season.ResultsTableType.ToString(),
                        season.EnableRunsScored,
                        season.EnableRunsConceded,
                        season.EnableLastPlayerBatsOn,
                        season.SeasonRoute
                    }
                    );

            foreach (var matchType in season.MatchTypes)
            {
                _connection.Execute($@"INSERT INTO {Tables.SeasonMatchType}
                    (SeasonMatchTypeId, SeasonId, MatchType)
                    VALUES
                    (@SeasonMatchTypeId, @SeasonId, @MatchType)",
                  new
                  {
                      SeasonMatchTypeId = Guid.NewGuid(),
                      season.SeasonId,
                      MatchType = matchType.ToString()
                  });
            }


            foreach (var rule in season.PointsRules)
            {
                _connection.Execute($@"INSERT INTO {Tables.PointsRule}
                    (PointsRuleId, SeasonId, MatchResultType, HomePoints, AwayPoints)
                    VALUES
                    (@PointsRuleId, @SeasonId, @MatchResultType, @HomePoints, @AwayPoints)",
                  new
                  {
                      rule.PointsRuleId,
                      season.SeasonId,
                      MatchResultType = rule.MatchResultType.ToString(),
                      rule.HomePoints,
                      rule.AwayPoints
                  });
            }

            foreach (var adjustment in season.PointsAdjustments)
            {
                _connection.Execute($@"INSERT INTO {Tables.PointsAdjustment}
                    (PointsAdjustmentId, SeasonId, TeamId, Points, Reason)
                    VALUES
                    (@PointsAdjustmentId, @SeasonId, @TeamId, @Points, @Reason)",
                  new
                  {
                      adjustment.PointsAdjustmentId,
                      season.SeasonId,
                      adjustment.Team.TeamId,
                      adjustment.Points,
                      adjustment.Reason
                  });
            }

            foreach (var overSet in season.DefaultOverSets)
            {
                CreateOverSet(overSet, null, season.SeasonId, null);
            }
        }

        public void CreatePlayerInMatchStatisticsRecord(PlayerInMatchStatisticsRecord record)
        {
            _connection.Execute($@"INSERT INTO {Tables.PlayerInMatchStatistics}
                    (PlayerInMatchStatisticsId, PlayerId, PlayerIdentityId, PlayerIdentityName, PlayerRoute, MatchId, MatchStartTime, MatchType, MatchPlayerType, MatchName, MatchRoute, 
                     TournamentId, SeasonId, CompetitionId, MatchTeamId, ClubId, TeamId, TeamName, TeamRoute, OppositionTeamId, OppositionTeamName, OppositionTeamRoute, MatchLocationId, 
                     MatchInningsPair, TeamRunsScored, TeamWicketsLost, TeamBonusOrPenaltyRunsAwarded, TeamRunsConceded, TeamNoBallsConceded, TeamWidesConceded, TeamByesConceded, TeamWicketsTaken, 
                     BowlingFiguresId, OverNumberOfFirstOverBowled, BallsBowled, Overs, Maidens, NoBalls, Wides, RunsConceded, HasRunsConceded, Wickets, WicketsWithBowling, 
                     WonToss, BattedFirst, PlayerInningsNumber, PlayerInningsId, BattingPosition, DismissalType, PlayerWasDismissed, BowledByPlayerIdentityId, BowledByPlayerIdentityName, BowledByPlayerRoute, 
                     CaughtByPlayerIdentityId, CaughtByPlayerIdentityName, CaughtByPlayerRoute, RunOutByPlayerIdentityId, RunOutByPlayerIdentityName, RunOutByPlayerRoute, 
                     RunsScored, BallsFaced, Catches, RunOuts, WonMatch, PlayerOfTheMatch)
                    VALUES
                    (@PlayerInMatchStatisticsId, @PlayerId, @PlayerIdentityId, 
                        (SELECT PlayerIdentityName FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @PlayerIdentityId), 
                        (SELECT PlayerRoute FROM {Tables.Player} WHERE PlayerId = @PlayerId), 
                     @MatchId, 
                        (SELECT StartTime FROM {Tables.Match} WHERE MatchId = @MatchId),
                        (SELECT MatchType FROM {Tables.Match} WHERE MatchId = @MatchId),
                        (SELECT PlayerType FROM {Tables.Match} WHERE MatchId = @MatchId),
                        (SELECT MatchName FROM {Tables.Match} WHERE MatchId = @MatchId),
                        (SELECT MatchRoute FROM {Tables.Match} WHERE MatchId = @MatchId),
                        (SELECT TournamentId FROM {Tables.Match} WHERE MatchId = @MatchId), 
                        (SELECT SeasonId FROM {Tables.Match} WHERE MatchId = @MatchId), 
                        (SELECT CompetitionId FROM {Tables.Match} m LEFT JOIN {Tables.Season} s ON m.SeasonId = s.SeasonId WHERE m.MatchId = @MatchId),
                     @MatchTeamId, 
                        (SELECT ClubId FROM {Tables.Team} WHERE TeamId = (SELECT TeamId FROM {Tables.MatchTeam} WHERE MatchTeamId = @MatchTeamId)),
                        (SELECT TeamId FROM {Tables.MatchTeam} WHERE MatchTeamId = @MatchTeamId), 
                        (SELECT dbo.fn_TeamName((SELECT TeamId FROM {Tables.MatchTeam} WHERE MatchTeamId = @MatchTeamId), (SELECT StartTime FROM {Tables.Match} WHERE MatchId = @MatchId))),
                        (SELECT TeamRoute FROM {Tables.Team} WHERE TeamId = (SELECT TeamId FROM {Tables.MatchTeam} WHERE MatchTeamId = @MatchTeamId)),
                     @OppositionTeamId, 
                        (SELECT dbo.fn_TeamName(@OppositionTeamId, (SELECT StartTime FROM {Tables.Match} WHERE MatchId = @MatchId))),
                        (SELECT TeamRoute FROM {Tables.Team} WHERE TeamId = @OppositionTeamId),
                        (SELECT MatchLocationId FROM {Tables.Match} WHERE MatchId = @MatchId),
                     @MatchInningsPair, @TeamRunsScored, @TeamWicketsLost, @TeamBonusOrPenaltyRunsAwarded, @TeamRunsConceded, @TeamNoBallsConceded, @TeamWidesConceded, @TeamByesConceded, @TeamWicketsTaken, 
                     @BowlingFiguresId, @OverNumberOfFirstOverBowled, @BallsBowled, @Overs, @Maidens, @NoBalls, @Wides, @RunsConceded, @HasRunsConceded, @Wickets, @WicketsWithBowling,
                     @WonToss, @BattedFirst, @PlayerInningsNumber, @PlayerInningsId, @BattingPosition, @DismissalType, @PlayerWasDismissed, @BowledByPlayerIdentityId, 
                        (SELECT CASE WHEN @BowledByPlayerIdentityId IS NULL THEN NULL ELSE (SELECT PlayerIdentityName FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @BowledByPlayerIdentityId) END),
                        (SELECT CASE WHEN @BowledByPlayerIdentityId IS NULL THEN NULL ELSE (SELECT PlayerRoute FROM {Tables.PlayerIdentity} pi INNER JOIN {Tables.Player} p ON pi.PlayerId = p.PlayerId WHERE PlayerIdentityId = @BowledByPlayerIdentityId) END),
                     @CaughtByPlayerIdentityId, 
                        (SELECT CASE WHEN @CaughtByPlayerIdentityId IS NULL THEN NULL ELSE (SELECT PlayerIdentityName FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @CaughtByPlayerIdentityId) END),
                        (SELECT CASE WHEN @CaughtByPlayerIdentityId IS NULL THEN NULL ELSE (SELECT PlayerRoute FROM {Tables.PlayerIdentity} pi INNER JOIN {Tables.Player} p ON pi.PlayerId = p.PlayerId WHERE PlayerIdentityId = @CaughtByPlayerIdentityId) END),
                     @RunOutByPlayerIdentityId, 
                        (SELECT CASE WHEN @RunOutByPlayerIdentityId IS NULL THEN NULL ELSE (SELECT PlayerIdentityName FROM {Tables.PlayerIdentity} WHERE PlayerIdentityId = @RunOutByPlayerIdentityId) END),
                        (SELECT CASE WHEN @RunOutByPlayerIdentityId IS NULL THEN NULL ELSE (SELECT PlayerRoute FROM {Tables.PlayerIdentity} pi INNER JOIN {Tables.Player} p ON pi.PlayerId = p.PlayerId WHERE PlayerIdentityId = @RunOutByPlayerIdentityId) END),
                     @RunsScored, @BallsFaced, @Catches, @RunOuts, @WonMatch, @PlayerOfTheMatch)",
                        new
                        {
                            PlayerInMatchStatisticsId = Guid.NewGuid(),
                            record.PlayerId,
                            record.PlayerIdentityId,
                            record.MatchId,
                            record.MatchTeamId,
                            record.OppositionTeamId,
                            record.MatchInningsPair,
                            record.TeamRunsScored,
                            record.TeamWicketsLost,
                            record.TeamBonusOrPenaltyRunsAwarded,
                            record.TeamRunsConceded,
                            record.TeamNoBallsConceded,
                            record.TeamWidesConceded,
                            record.TeamByesConceded,
                            record.TeamWicketsTaken,
                            record.BowlingFiguresId,
                            record.OverNumberOfFirstOverBowled,
                            record.BallsBowled,
                            record.Overs,
                            record.Maidens,
                            record.NoBalls,
                            record.Wides,
                            record.RunsConceded,
                            record.HasRunsConceded,
                            record.Wickets,
                            record.WicketsWithBowling,
                            record.WonToss,
                            record.BattedFirst,
                            record.PlayerInningsNumber,
                            record.PlayerInningsId,
                            record.BattingPosition,
                            record.DismissalType,
                            record.PlayerWasDismissed,
                            record.BowledByPlayerIdentityId,
                            record.CaughtByPlayerIdentityId,
                            record.RunOutByPlayerIdentityId,
                            record.RunsScored,
                            record.BallsFaced,
                            record.Catches,
                            record.RunOuts,
                            record.WonMatch,
                            record.PlayerOfTheMatch
                        });
        }
    }
}
