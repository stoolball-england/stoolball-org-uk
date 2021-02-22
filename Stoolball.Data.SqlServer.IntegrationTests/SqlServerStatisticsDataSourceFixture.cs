using System;
using System.Collections.Generic;
using System.Linq;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    public sealed class SqlServerStatisticsDataSourceFixture : BaseSqlServerFixture
    {
        public Player PlayerWithMultipleIdentities { get; private set; }
        public List<Match> MatchesForPlayerWithMultipleIdentities { get; internal set; } = new List<Match>();

        public List<PlayerIdentity> PlayerIdentities { get; internal set; } = new List<PlayerIdentity>();

        public SqlServerStatisticsDataSourceFixture() : base("StoolballStatisticsDataSourceIntegrationTests")
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

                // Create a player with multiple identities, and a couple of matches that feature that player
                PlayerWithMultipleIdentities = seedDataGenerator.CreatePlayer("Player identity 1");
                PlayerWithMultipleIdentities.PlayerIdentities.Add(seedDataGenerator.CreatePlayer("Player identity 2").PlayerIdentities.First());

                repo.CreatePlayer(PlayerWithMultipleIdentities);
                foreach (var identity in PlayerWithMultipleIdentities.PlayerIdentities)
                {
                    repo.CreateTeam(identity.Team);
                    PlayerIdentities.Add(identity);
                }
                MatchesForPlayerWithMultipleIdentities.Add(seedDataGenerator.CreateMatchInThePastWithMinimalDetails());
                MatchesForPlayerWithMultipleIdentities.Add(seedDataGenerator.CreateMatchInThePastWithMinimalDetails());
                for (var i = 0; i < MatchesForPlayerWithMultipleIdentities.Count; i++)
                {
                    MatchesForPlayerWithMultipleIdentities[i].StartTime = MatchesForPlayerWithMultipleIdentities[i].StartTime.AddMonths(i);
                }
                foreach (var identity in PlayerWithMultipleIdentities.PlayerIdentities)
                {
                    identity.Player = PlayerWithMultipleIdentities;
                    identity.TotalMatches = MatchesForPlayerWithMultipleIdentities.Count;
                    identity.FirstPlayed = MatchesForPlayerWithMultipleIdentities.Min(x => x.StartTime);
                    identity.LastPlayed = MatchesForPlayerWithMultipleIdentities.Max(x => x.StartTime);
                    repo.CreatePlayerIdentity(identity);
                }
                foreach (var match in MatchesForPlayerWithMultipleIdentities)
                {
                    var playerIdentitiesInMatch = playerIdentityFinder.PlayerIdentitiesInMatch(match).ToList();
                    CreateMatchWithStatisticsForPlayer(PlayerWithMultipleIdentities, match, repo, playerIdentitiesInMatch);
                }

                // Create utilities to randomise and build data
                var randomiser = new Random();
                var statisticsBuilder = new PlayerInMatchStatisticsBuilder(new PlayerIdentityFinder(), new OversHelper());

                // Create a pool of teams of 8 players
                var poolOfTeams = new List<(Team, List<PlayerIdentity>)>();
                for (var i = 0; i < 5; i++)
                {
                    poolOfTeams.Add(CreateATeamWithPlayers(seedDataGenerator, $"Team {i + 1} pool player"));
                }

                // Create 10 matches for them to play in, with some first innings batting
                var poolOfMatches = new List<Match>();
                for (var i = 0; i < 10; i++)
                {
                    var homeTeamBatsFirst = randomiser.Next(2) == 0;

                    var (teamA, teamAPlayers) = poolOfTeams[randomiser.Next(poolOfTeams.Count)];
                    (Team teamB, List<PlayerIdentity> teamBPlayers) = (null, null);
                    do
                    {
                        (teamB, teamBPlayers) = poolOfTeams[randomiser.Next(poolOfTeams.Count)];
                    }
                    while (teamA.TeamId == teamB.TeamId);

                    var teamAInMatch = new TeamInMatch
                    {
                        MatchTeamId = Guid.NewGuid(),
                        TeamRole = homeTeamBatsFirst ? TeamRole.Home : TeamRole.Away,
                        Team = teamA,
                        WonToss = randomiser.Next(2) == 0,
                        BattedFirst = true
                    };

                    var teamBInMatch = new TeamInMatch
                    {
                        MatchTeamId = Guid.NewGuid(),
                        TeamRole = homeTeamBatsFirst ? TeamRole.Away : TeamRole.Home,
                        Team = teamB,
                        WonToss = !teamAInMatch.WonToss,
                        BattedFirst = false
                    };

                    poolOfMatches.Add(seedDataGenerator.CreateMatchInThePastWithMinimalDetails());
                    poolOfMatches[i].StartTime = DateTimeOffset.UtcNow.AddMonths((i * -1) - 1);
                    poolOfMatches[i].Teams.Add(teamAInMatch);
                    poolOfMatches[i].Teams.Add(teamBInMatch);

                    CreateRandomScorecardData(randomiser, poolOfMatches[i].MatchInnings[0], teamAInMatch, teamBInMatch, teamAPlayers, teamBPlayers);
                    CreateRandomScorecardData(randomiser, poolOfMatches[i].MatchInnings[1], teamBInMatch, teamAInMatch, teamBPlayers, teamAPlayers);
                }

                // Add all of that to the database
                var teamsThatGotUsed = poolOfMatches.SelectMany(x => x.Teams).Select(x => x.Team.TeamId.Value).Distinct().ToList();
                foreach (var (team, playerIdentities) in poolOfTeams)
                {
                    if (!teamsThatGotUsed.Contains(team.TeamId.Value)) continue;

                    repo.CreateTeam(team);
                    foreach (var playerIdentity in playerIdentities)
                    {
                        repo.CreatePlayer(playerIdentity.Player);
                        repo.CreatePlayerIdentity(playerIdentity);
                    }
                }
                foreach (var match in poolOfMatches)
                {
                    repo.CreateMatch(match);

                    var statisticsRecords = statisticsBuilder.BuildStatisticsForMatch(match);
                    foreach (var record in statisticsRecords)
                    {
                        repo.CreatePlayerInMatchStatisticsRecord(record);
                    }
                }

                // Also add it to expected collections for testing
                foreach (var (team, playerIdentities) in poolOfTeams)
                {
                    PlayerIdentities.AddRange(playerIdentities);
                }
            }
        }

        private static void CreateRandomScorecardData(Random randomiser, MatchInnings innings, TeamInMatch battingTeam, TeamInMatch fieldingTeam, List<PlayerIdentity> battingPlayers, List<PlayerIdentity> fieldingPlayers)
        {
            innings.BattingTeam = battingTeam;
            innings.BowlingTeam = fieldingTeam;

            for (var p = 0; p < battingPlayers.Count; p++)
            {
                var fielderOrMissingData = randomiser.Next(2) == 0 ? fieldingPlayers[randomiser.Next(fieldingPlayers.Count)] : null;
                var bowlerOrMissingData = randomiser.Next(2) == 0 ? fieldingPlayers[randomiser.Next(fieldingPlayers.Count)] : null;
                innings.PlayerInnings.Add(CreateRandomPlayerInnings(randomiser, battingPlayers[p], fielderOrMissingData, bowlerOrMissingData));
            }

            // sometimes pick a random player to bat twice in the innings
            if (randomiser.Next(2) == 0)
            {
                innings.PlayerInnings.Add(CreateRandomPlayerInnings(randomiser, battingPlayers[randomiser.Next(battingPlayers.Count)], fieldingPlayers[randomiser.Next(fieldingPlayers.Count)], fieldingPlayers[randomiser.Next(fieldingPlayers.Count)]));
            }
        }

        private static PlayerInnings CreateRandomPlayerInnings(Random randomiser, PlayerIdentity batter, PlayerIdentity fielderOrMissingData, PlayerIdentity bowlerOrMissingData)
        {
            var dismissalTypes = Enum.GetValues(typeof(DismissalType));
            var dismissal = (DismissalType)dismissalTypes.GetValue(randomiser.Next(dismissalTypes.Length));
            if (dismissal != DismissalType.Caught || dismissal != DismissalType.RunOut)
            {
                fielderOrMissingData = null;
            }
            if (!StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(dismissal))
            {
                bowlerOrMissingData = null;
            }

            return new PlayerInnings
            {
                PlayerInningsId = Guid.NewGuid(),
                Batter = batter,
                DismissalType = dismissal,
                DismissedBy = fielderOrMissingData,
                Bowler = bowlerOrMissingData,
                RunsScored = randomiser.Next(2) == 0 ? randomiser.Next(102) : (int?)null, // simulate missing data
                BallsFaced = randomiser.Next(2) == 0 ? randomiser.Next(151) : (int?)null, // simulate missing data
            };
        }

        private static (Team, List<PlayerIdentity>) CreateATeamWithPlayers(SeedDataGenerator seedDataGenerator, string playerName)
        {
            var poolOfPlayers = new List<PlayerIdentity>();
            for (var i = 0; i < 8; i++)
            {
                var player = seedDataGenerator.CreatePlayer($"{playerName} {i + 1}");
                var playerIdentity = player.PlayerIdentities.First();
                playerIdentity.Player = player;
                poolOfPlayers.Add(playerIdentity);
                poolOfPlayers[i].Team = poolOfPlayers[0].Team;
            }

            return (poolOfPlayers[0].Team, poolOfPlayers);
        }

        private static void CreateMatchWithStatisticsForPlayer(Player player, Match match, SqlServerIntegrationTestsRepository repo, List<PlayerIdentity> playerIdentities)
        {
            match.Teams.Add(new TeamInMatch { MatchTeamId = Guid.NewGuid(), TeamRole = TeamRole.Home, Team = player.PlayerIdentities.First().Team });
            match.Teams.Add(new TeamInMatch { MatchTeamId = Guid.NewGuid(), TeamRole = TeamRole.Away, Team = player.PlayerIdentities.Last().Team });
            foreach (var team in match.Teams)
            {
                repo.CreateTeam(team.Team);
            }
            foreach (var playerInMatch in playerIdentities)
            {
                repo.CreatePlayer(playerInMatch.Player);
                repo.CreatePlayerIdentity(playerInMatch);
            }
            repo.CreateMatch(match);
            repo.CreatePlayerInMatchStatisticsRecord(new PlayerInMatchStatisticsRecord
            {
                PlayerInMatchStatisticsId = Guid.NewGuid(),
                PlayerId = player.PlayerId.Value,
                PlayerIdentityId = player.PlayerIdentities.First().PlayerIdentityId.Value,
                PlayerIdentityName = player.PlayerIdentities.First().PlayerIdentityName,
                PlayerRoute = player.PlayerRoute,
                TeamId = player.PlayerIdentities.First().Team.TeamId.Value,
                TeamName = player.PlayerIdentities.First().Team.TeamName,
                TeamRoute = player.PlayerIdentities.First().Team.TeamRoute,
                OppositionTeamId = player.PlayerIdentities.Last().Team.TeamId.Value,
                OppositionTeamName = player.PlayerIdentities.Last().Team.TeamName,
                OppositionTeamRoute = player.PlayerIdentities.Last().Team.TeamRoute,
                MatchId = match.MatchId.Value,
                MatchName = match.MatchName,
                MatchRoute = match.MatchRoute,
                MatchStartTime = match.StartTime,
                MatchInningsPair = 1,
                MatchTeamId = match.Teams.First().MatchTeamId.Value
            });
            repo.CreatePlayerInMatchStatisticsRecord(new PlayerInMatchStatisticsRecord
            {
                PlayerInMatchStatisticsId = Guid.NewGuid(),
                PlayerId = player.PlayerId.Value,
                PlayerIdentityId = player.PlayerIdentities.Last().PlayerIdentityId.Value,
                PlayerIdentityName = player.PlayerIdentities.Last().PlayerIdentityName,
                PlayerRoute = player.PlayerRoute,
                TeamId = player.PlayerIdentities.Last().Team.TeamId.Value,
                TeamName = player.PlayerIdentities.Last().Team.TeamName,
                TeamRoute = player.PlayerIdentities.Last().Team.TeamRoute,
                OppositionTeamId = player.PlayerIdentities.First().Team.TeamId.Value,
                OppositionTeamName = player.PlayerIdentities.First().Team.TeamName,
                OppositionTeamRoute = player.PlayerIdentities.First().Team.TeamRoute,
                MatchId = match.MatchId.Value,
                MatchName = match.MatchName,
                MatchRoute = match.MatchRoute,
                MatchStartTime = match.StartTime,
                MatchInningsPair = 1,
                MatchTeamId = match.Teams.Last().MatchTeamId.Value
            });
        }
    }
}
