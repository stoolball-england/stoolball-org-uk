﻿using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    public sealed class SqlServerDataSourceFixture : BaseSqlServerFixture
    {
        public Match MatchInThePastWithMinimalDetails { get; private set; }
        public Match MatchInTheFutureWithMinimalDetails { get; private set; }

        public Match MatchInThePastWithFullDetails { get; private set; }
        public Match MatchInThePastWithFullDetailsAndTournament { get; private set; }
        public Tournament TournamentInThePastWithMinimalDetails { get; private set; }
        public Tournament TournamentInThePastWithFullDetails { get; private set; }
        public Tournament TournamentInTheFutureWithMinimalDetails { get; private set; }
        public Competition CompetitionWithMinimalDetails { get; private set; }
        public Competition CompetitionWithFullDetails { get; private set; }
        public Season SeasonWithMinimalDetails { get; private set; }
        public MatchLocation MatchLocationWithMinimalDetails { get; private set; }
        public MatchLocation MatchLocationWithFullDetails { get; private set; }
        public Club ClubWithMinimalDetails { get; private set; }
        public Club ClubWithTeams { get; private set; }
        public Team TeamWithMinimalDetails { get; private set; }
        public Team TeamWithFullDetails { get; private set; }
        public List<Competition> Competitions { get; internal set; } = new List<Competition>();
        public List<Season> Seasons { get; internal set; } = new List<Season>();
        public List<Match> Matches { get; internal set; } = new List<Match>();
        public List<MatchListing> MatchListings { get; internal set; } = new List<MatchListing>();
        public List<MatchLocation> MatchLocations { get; internal set; } = new List<MatchLocation>();
        public List<Team> Teams { get; internal set; } = new List<Team>();

        public List<PlayerIdentity> PlayerIdentities { get; internal set; } = new List<PlayerIdentity>();
        public Season SeasonWithFullDetails { get; private set; }
        public List<MatchListing> TournamentMatchListings { get; private set; } = new List<MatchListing>();

        public SqlServerDataSourceFixture() : base("StoolballDataSourceIntegrationTests")
        {
            // Populate seed data so that there's a consistent baseline for each test run
            SeedDatabase();
        }

        protected override void SeedDatabase()
        {
            var seedDataGenerator = new SeedDataGenerator();
            var playerIdentityFinder = new PlayerIdentityFinder();
            using (var connection = ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();

                var repo = new SqlServerIntegrationTestsRepository(connection);

                ClubWithMinimalDetails = seedDataGenerator.CreateClubWithMinimalDetails();
                repo.CreateClub(ClubWithMinimalDetails);

                ClubWithTeams = seedDataGenerator.CreateClubWithTeams();
                repo.CreateClub(ClubWithTeams);
                Teams.AddRange(ClubWithTeams.Teams);

                TeamWithMinimalDetails = seedDataGenerator.CreateTeamWithMinimalDetails("Team minimal");
                repo.CreateTeam(TeamWithMinimalDetails);
                Teams.Add(TeamWithMinimalDetails);

                TeamWithFullDetails = seedDataGenerator.CreateTeamWithFullDetails();
                repo.CreateTeam(TeamWithFullDetails);
                Teams.Add(TeamWithFullDetails);
                foreach (var matchLocation in TeamWithFullDetails.MatchLocations)
                {
                    repo.CreateMatchLocation(matchLocation);
                    repo.AddTeamToMatchLocation(TeamWithFullDetails, matchLocation);
                    MatchLocations.Add(matchLocation);
                }
                repo.CreateCompetition(TeamWithFullDetails.Seasons[0].Season.Competition);
                Competitions.Add(TeamWithFullDetails.Seasons[0].Season.Competition);
                foreach (var season in TeamWithFullDetails.Seasons)
                {
                    repo.CreateSeason(season.Season, season.Season.Competition.CompetitionId.Value);
                    repo.AddTeamToSeason(season);
                    Seasons.Add(season.Season);
                }

                MatchInThePastWithMinimalDetails = seedDataGenerator.CreateMatchInThePastWithMinimalDetails();
                repo.CreateMatch(MatchInThePastWithMinimalDetails);
                Matches.Add(MatchInThePastWithMinimalDetails);
                MatchListings.Add(MatchToMatchListing(MatchInThePastWithMinimalDetails));

                MatchInTheFutureWithMinimalDetails = seedDataGenerator.CreateMatchInThePastWithMinimalDetails();
                MatchInTheFutureWithMinimalDetails.StartTime = DateTime.UtcNow.AddMonths(1);
                repo.CreateMatch(MatchInTheFutureWithMinimalDetails);
                Matches.Add(MatchInTheFutureWithMinimalDetails);
                MatchListings.Add(MatchToMatchListing(MatchInTheFutureWithMinimalDetails));

                MatchInThePastWithFullDetails = seedDataGenerator.CreateMatchInThePastWithFullDetails();
                repo.CreateMatchLocation(MatchInThePastWithFullDetails.MatchLocation);
                var playersForMatchInThePastWithFullDetails = playerIdentityFinder.PlayerIdentitiesInMatch(MatchInThePastWithFullDetails);
                foreach (var team in MatchInThePastWithFullDetails.Teams)
                {
                    repo.CreateTeam(team.Team);
                }
                foreach (var player in playersForMatchInThePastWithFullDetails)
                {
                    repo.CreatePlayer(player.Player);
                    repo.CreatePlayerIdentity(player);
                }
                repo.CreateMatch(MatchInThePastWithFullDetails);
                Teams.AddRange(MatchInThePastWithFullDetails.Teams.Select(x => x.Team));
                Competitions.Add(MatchInThePastWithFullDetails.Season.Competition);
                Seasons.Add(MatchInThePastWithFullDetails.Season);
                MatchLocations.Add(MatchInThePastWithFullDetails.MatchLocation);
                Matches.Add(MatchInThePastWithFullDetails);
                MatchListings.Add(MatchToMatchListing(MatchInThePastWithFullDetails));
                PlayerIdentities.AddRange(playerIdentityFinder.PlayerIdentitiesInMatch(MatchInThePastWithFullDetails));

                TournamentInThePastWithMinimalDetails = seedDataGenerator.CreateTournamentInThePastWithMinimalDetails();
                repo.CreateTournament(TournamentInThePastWithMinimalDetails);
                MatchListings.Add(TournamentToMatchListing(TournamentInThePastWithMinimalDetails));

                TournamentInTheFutureWithMinimalDetails = seedDataGenerator.CreateTournamentInThePastWithMinimalDetails();
                TournamentInTheFutureWithMinimalDetails.StartTime = DateTime.UtcNow.AddMonths(1);
                repo.CreateTournament(TournamentInTheFutureWithMinimalDetails);
                MatchListings.Add(TournamentToMatchListing(TournamentInTheFutureWithMinimalDetails));

                TournamentInThePastWithFullDetails = seedDataGenerator.CreateTournamentInThePastWithFullDetails();
                Teams.AddRange(TournamentInThePastWithFullDetails.Teams.Select(x => x.Team));
                foreach (var team in TournamentInThePastWithFullDetails.Teams)
                {
                    repo.CreateTeam(team.Team);
                }
                MatchLocations.Add(TournamentInThePastWithFullDetails.TournamentLocation);
                repo.CreateMatchLocation(TournamentInThePastWithFullDetails.TournamentLocation);
                repo.CreateTournament(TournamentInThePastWithFullDetails);
                foreach (var team in TournamentInThePastWithFullDetails.Teams)
                {
                    repo.AddTeamToTournament(team, TournamentInThePastWithFullDetails);
                }
                foreach (var season in TournamentInThePastWithFullDetails.Seasons)
                {
                    repo.CreateCompetition(season.Competition);
                    Competitions.Add(season.Competition);
                    repo.CreateSeason(season, season.Competition.CompetitionId.Value);
                    repo.AddTournamentToSeason(TournamentInThePastWithFullDetails, season);
                    Seasons.Add(season);
                }
                MatchListings.Add(TournamentToMatchListing(TournamentInThePastWithFullDetails));
                for (var i = 0; i < 5; i++)
                {
                    var tournamentMatch = seedDataGenerator.CreateMatchInThePastWithMinimalDetails();
                    tournamentMatch.Tournament = TournamentInThePastWithFullDetails;
                    tournamentMatch.StartTime = TournamentInThePastWithFullDetails.StartTime.AddHours(i);
                    tournamentMatch.OrderInTournament = i + 1;
                    repo.CreateMatch(tournamentMatch);
                    Matches.Add(tournamentMatch);
                    TournamentMatchListings.Add(MatchToMatchListing(tournamentMatch));
                }

                MatchInThePastWithFullDetailsAndTournament = seedDataGenerator.CreateMatchInThePastWithFullDetails();
                MatchInThePastWithFullDetailsAndTournament.Tournament = TournamentInThePastWithMinimalDetails;
                MatchInThePastWithFullDetailsAndTournament.Season.FromYear = MatchInThePastWithFullDetailsAndTournament.Season.UntilYear = 2018;
                repo.CreateMatchLocation(MatchInThePastWithFullDetailsAndTournament.MatchLocation);
                foreach (var team in MatchInThePastWithFullDetailsAndTournament.Teams)
                {
                    repo.CreateTeam(team.Team);
                }
                var playersForMatchInThePastWithFullDetailsAndTournament = playerIdentityFinder.PlayerIdentitiesInMatch(MatchInThePastWithFullDetailsAndTournament);
                foreach (var player in playersForMatchInThePastWithFullDetailsAndTournament)
                {
                    repo.CreatePlayer(player.Player);
                    repo.CreatePlayerIdentity(player);
                }
                repo.CreateMatch(MatchInThePastWithFullDetailsAndTournament);
                Teams.AddRange(MatchInThePastWithFullDetailsAndTournament.Teams.Select(x => x.Team));
                Competitions.Add(MatchInThePastWithFullDetailsAndTournament.Season.Competition);
                MatchLocations.Add(MatchInThePastWithFullDetailsAndTournament.MatchLocation);
                Matches.Add(MatchInThePastWithFullDetailsAndTournament);
                TournamentMatchListings.Add(MatchToMatchListing(MatchInThePastWithFullDetailsAndTournament));
                Seasons.Add(MatchInThePastWithFullDetailsAndTournament.Season);
                PlayerIdentities.AddRange(playerIdentityFinder.PlayerIdentitiesInMatch(MatchInThePastWithFullDetailsAndTournament));

                CompetitionWithMinimalDetails = seedDataGenerator.CreateCompetitionWithMinimalDetails();
                repo.CreateCompetition(CompetitionWithMinimalDetails);
                Competitions.Add(CompetitionWithMinimalDetails);

                CompetitionWithFullDetails = seedDataGenerator.CreateCompetitionWithFullDetails();
                repo.CreateCompetition(CompetitionWithFullDetails);
                foreach (var season in CompetitionWithFullDetails.Seasons)
                {
                    repo.CreateSeason(season, CompetitionWithFullDetails.CompetitionId.Value);
                    Seasons.Add(season);
                }
                Competitions.Add(CompetitionWithFullDetails);

                var competitionForSeason = seedDataGenerator.CreateCompetitionWithMinimalDetails();
                competitionForSeason.UntilYear = 2021;
                SeasonWithMinimalDetails = seedDataGenerator.CreateSeasonWithMinimalDetails(competitionForSeason, 2020, 2020);
                competitionForSeason.Seasons.Add(SeasonWithMinimalDetails);
                repo.CreateCompetition(competitionForSeason);
                repo.CreateSeason(SeasonWithMinimalDetails, competitionForSeason.CompetitionId.Value);
                Competitions.Add(competitionForSeason);
                Seasons.Add(SeasonWithMinimalDetails);

                SeasonWithFullDetails = seedDataGenerator.CreateSeasonWithFullDetails(competitionForSeason, 2021, 2021);
                competitionForSeason.Seasons.Add(SeasonWithFullDetails);
                Teams.AddRange(SeasonWithFullDetails.Teams.Select(x => x.Team));
                foreach (var team in SeasonWithFullDetails.Teams)
                {
                    repo.CreateTeam(team.Team);
                }
                repo.CreateSeason(SeasonWithFullDetails, competitionForSeason.CompetitionId.Value);
                foreach (var team in SeasonWithFullDetails.Teams)
                {
                    repo.AddTeamToSeason(team);
                }
                Seasons.Add(SeasonWithFullDetails);

                MatchLocationWithMinimalDetails = seedDataGenerator.CreateMatchLocationWithMinimalDetails();
                repo.CreateMatchLocation(MatchLocationWithMinimalDetails);
                MatchLocations.Add(MatchLocationWithMinimalDetails);

                MatchLocationWithFullDetails = seedDataGenerator.CreateMatchLocationWithFullDetails();
                repo.CreateMatchLocation(MatchLocationWithFullDetails);
                Teams.AddRange(MatchLocationWithFullDetails.Teams);
                MatchLocations.Add(MatchLocationWithFullDetails);


                for (var i = 0; i < 30; i++)
                {
                    var competition = seedDataGenerator.CreateCompetitionWithMinimalDetails();
                    repo.CreateCompetition(competition);
                    Competitions.Add(competition);

                    var matchLocation = seedDataGenerator.CreateMatchLocationWithMinimalDetails();
                    repo.CreateMatchLocation(matchLocation);
                    MatchLocations.Add(matchLocation);

                    var match = seedDataGenerator.CreateMatchInThePastWithMinimalDetails();
                    match.MatchLocation = matchLocation;
                    match.StartTime = DateTime.UtcNow.AddMonths(i - 15);
                    match.MatchType = (i % 2 == 0) ? MatchType.FriendlyMatch : MatchType.LeagueMatch;
                    match.PlayerType = (i % 3 == 0) ? PlayerType.Mixed : PlayerType.Ladies;
                    repo.CreateMatch(match);
                    Matches.Add(match);
                    MatchListings.Add(MatchToMatchListing(match));

                    var tournament = seedDataGenerator.CreateTournamentInThePastWithMinimalDetails();
                    tournament.TournamentLocation = matchLocation;
                    tournament.StartTime = DateTime.UtcNow.AddMonths(i - 20).AddDays(5);
                    repo.CreateTournament(tournament);
                    MatchListings.Add(TournamentToMatchListing(tournament));
                }
            }
        }
        private static MatchListing TournamentToMatchListing(Tournament tournament)
        {
            return new MatchListing
            {
                MatchName = tournament.TournamentName,
                MatchRoute = tournament.TournamentRoute,
                StartTime = tournament.StartTime,
                StartTimeIsKnown = tournament.StartTimeIsKnown,
                PlayerType = tournament.PlayerType,
                TournamentQualificationType = tournament.QualificationType,
                SpacesInTournament = tournament.SpacesInTournament
            };
        }

        private static MatchListing MatchToMatchListing(Match match)
        {
            return new MatchListing
            {
                MatchInnings = match.MatchInnings,
                MatchName = match.MatchName,
                MatchRoute = match.MatchRoute,
                MatchResultType = match.MatchResultType,
                MatchType = match.MatchType,
                PlayerType = match.PlayerType,
                StartTime = match.StartTime,
                StartTimeIsKnown = match.StartTimeIsKnown,
                Teams = match.Teams
            };
        }
    }
}