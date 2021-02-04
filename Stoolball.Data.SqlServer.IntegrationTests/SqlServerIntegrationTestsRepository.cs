using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using Stoolball.Awards;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    public class SqlServerIntegrationTestsRepository
    {
        private readonly IDbConnection _connection;

        public SqlServerIntegrationTestsRepository(IDbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
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
                       tournament.StartTime,
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
                        UntilDate = club.UntilDate
                    });

            foreach (var team in club.Teams)
            {
                CreateTeam(team);
            }
        }

        public void CreateMatch(Match match)
        {
            if (match.MatchLocation != null)
            {
                CreateMatchLocation(match.MatchLocation);
            }

            if (match.Season != null)
            {
                CreateCompetition(match.Season.Competition);
            }

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
                match.StartTime,
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
                CreateTeam(teamInMatch.Team);

                _connection.Execute($@"INSERT INTO {Tables.MatchTeam} 
                    (MatchTeamId, TeamId, PlayingAsTeamName, MatchId, TeamRole, WonToss)
                    VALUES
                    (@MatchTeamId, @TeamId, @PlayingAsTeamName, @MatchId, @TeamRole, @WonToss)",
                   new
                   {
                       teamInMatch.MatchTeamId,
                       teamInMatch.Team.TeamId,
                       teamInMatch.PlayingAsTeamName,
                       match.MatchId,
                       TeamRole = teamInMatch.TeamRole.ToString(),
                       teamInMatch.WonToss
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

            var playerIdentities = new List<PlayerIdentity>();
            playerIdentities.AddRange(match.Awards.Select(x => x.PlayerIdentity));
            foreach (var innings in match.MatchInnings)
            {
                playerIdentities.AddRange(innings.PlayerInnings.Select(x => x.PlayerIdentity));
                playerIdentities.AddRange(innings.PlayerInnings.Select(x => x.DismissedBy).OfType<PlayerIdentity>());
                playerIdentities.AddRange(innings.PlayerInnings.Select(x => x.Bowler).OfType<PlayerIdentity>());
                playerIdentities.AddRange(innings.OversBowled.Select(x => x.PlayerIdentity));
                playerIdentities.AddRange(innings.BowlingFigures.Select(x => x.Bowler));
            }
            foreach (var playerIdentity in playerIdentities.Distinct(new PlayerIdentityEqualityComparer()))
            {
                CreatePlayerIdentity(playerIdentity);
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
                    (PlayerInningsId, MatchInningsId, PlayerIdentityId, BattingPosition, DismissalType, DismissedById, BowlerId, RunsScored, BallsFaced)
                    VALUES
                    (@PlayerInningsId, @MatchInningsId, @PlayerIdentityId, @BattingPosition, @DismissalType, @DismissedById, @BowlerId, @RunsScored, @BallsFaced)",
                  new
                  {
                      playerInnings.PlayerInningsId,
                      innings.MatchInningsId,
                      playerInnings.PlayerIdentity.PlayerIdentityId,
                      playerInnings.BattingPosition,
                      DismissalType = playerInnings.DismissalType.ToString(),
                      DismissedById = playerInnings.DismissedBy?.PlayerIdentityId,
                      BowlerId = playerInnings.Bowler?.PlayerIdentityId,
                      playerInnings.RunsScored,
                      playerInnings.BallsFaced
                  });
                }

                foreach (var overSet in innings.OverSets)
                {
                    CreateOverSet(overSet, innings.MatchInningsId, null);
                }

                foreach (var overBowled in innings.OversBowled)
                {
                    _connection.Execute($@"INSERT INTO {Tables.Over} 
                    (OverId, MatchInningsId, OverSetId, PlayerIdentityId, OverNumber, BallsBowled, NoBalls, Wides, RunsConceded)
                    VALUES
                    (@OverId, @MatchInningsId, @OverSetId, @PlayerIdentityId, @OverNumber, @BallsBowled, @NoBalls, @Wides, @RunsConceded)",
                  new
                  {
                      overBowled.OverId,
                      innings.MatchInningsId,
                      overBowled.OverSet.OverSetId,
                      overBowled.PlayerIdentity.PlayerIdentityId,
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
                    (BowlingFiguresId, MatchInningsId, BowlingOrder, PlayerIdentityId, Overs, Maidens, RunsConceded, Wickets, IsFromOversBowled)
                    VALUES
                    (@BowlingFiguresId, @MatchInningsId, @BowlingOrder, @PlayerIdentityId, @Overs, @Maidens, @RunsConceded, @Wickets, @IsFromOversBowled)",
                  new
                  {
                      bowlingFigures.BowlingFiguresId,
                      innings.MatchInningsId,
                      BowlingOrder = 1,
                      bowlingFigures.Bowler.PlayerIdentityId,
                      bowlingFigures.Overs,
                      bowlingFigures.Maidens,
                      bowlingFigures.RunsConceded,
                      bowlingFigures.Wickets,
                      IsFromOversBowled = true
                  });
                    i++;
                }
            }
        }

        public void AddTeamToMatchLocation(Team team, MatchLocation matchLocation)
        {
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
                teamInSeason.WithdrawnDate
            });
        }

        public void CreateOverSet(OverSet overSet, Guid? matchInningsId, Guid? seasonId)
        {
            _connection.Execute($@"INSERT INTO {Tables.OverSet} 
                    (OverSetId, MatchInningsId, SeasonId, OverSetNumber, Overs, BallsPerOver)
                    VALUES
                    (@OverSetId, @MatchInningsId, @SeasonId, @OverSetNumber, @Overs, @BallsPerOver)",
            new
            {
                overSet.OverSetId,
                MatchInningsId = matchInningsId,
                SeasonId = seasonId,
                overSet.OverSetNumber,
                overSet.Overs,
                overSet.BallsPerOver
            });
        }

        public void CreatePlayerIdentity(PlayerIdentity playerIdentity)
        {
            var playerId = Guid.NewGuid();
            _connection.Execute($@"INSERT INTO {Tables.Player} 
                    (PlayerId, PlayerName, PlayerRoute)
                    VALUES
                    (@PlayerId, @PlayerName, @PlayerRoute)",
                   new
                   {
                       PlayerId = playerId,
                       PlayerName = playerIdentity.PlayerIdentityName,
                       PlayerRoute = "/players/" + playerId
                   });

            _connection.Execute($@"INSERT INTO {Tables.PlayerIdentity} 
                    (PlayerIdentityId, PlayerId, TeamId, PlayerIdentityName, ComparableName, FirstPlayed, LastPlayed, TotalMatches, MissedMatches, Probability)
                    VALUES
                    (@PlayerIdentityId, @PlayerId, @TeamId, @PlayerIdentityName, @ComparableName, @FirstPlayed, @LastPlayed, @TotalMatches, @MissedMatches, @Probability)",
                   new
                   {
                       playerIdentity.PlayerIdentityId,
                       PlayerId = playerId,
                       playerIdentity.Team.TeamId,
                       playerIdentity.PlayerIdentityName,
                       ComparableName = playerIdentity.ComparableName(),
                       playerIdentity.FirstPlayed,
                       playerIdentity.LastPlayed,
                       playerIdentity.TotalMatches,
                       playerIdentity.MissedMatches,
                       playerIdentity.Probability
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

                _connection.Execute($@"INSERT INTO {Tables.TeamMatchLocation} 
                    (TeamMatchLocationId, TeamId, MatchLocationId, FromDate, UntilDate)
                    VALUES
                    (@TeamMatchLocationId, @TeamId, @MatchLocationId, @FromDate, @UntilDate)",
                    new
                    {
                        TeamMatchLocationId = Guid.NewGuid(),
                        team.TeamId,
                        matchLocation.MatchLocationId,
                        FromDate = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero),
                        UntilDate = (DateTimeOffset?)null
                    });
            }
        }

        public void CreateTeam(Team team)
        {
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

            foreach (var season in competition.Seasons)
            {
                CreateSeason(season, competition.CompetitionId.Value);
            }
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

            foreach (var overSet in season.DefaultOverSets)
            {
                CreateOverSet(overSet, null, season.SeasonId);
            }
        }
    }
}
