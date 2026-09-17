using Stoolball.Logging;
using Stoolball.Testing.ClubDataProviders;
using Stoolball.Testing.MatchDataProviders;
using Stoolball.Testing.PlayerDataProviders;
using Stoolball.Testing.SchoolDataProviders;
using Stoolball.Testing.TournamentDataProviders;

namespace Stoolball.Testing
{
    internal class SeedDataGenerator
    {
        private readonly Randomiser _randomiser;
        private readonly IBowlingFiguresCalculator _bowlingFiguresCalculator;
        private readonly IPlayerIdentityFinder _playerIdentityFinder;
        private readonly IMatchFinder _matchFinder;
        private readonly CompetitionFactory _competitionFactory;
        private readonly SeasonFactory _seasonFactory;
        private readonly TeamFactory _teamFactory;
        private readonly MatchLocationFactory _matchLocationFactory;
        private readonly PlayerFactory _playerFactory;
        private readonly CommentFactory _commentFactory;
        private readonly MatchFactory _matchFactory;
        private readonly IEnumerable<BaseMatchDataProvider> _matchDataProviders;
        private readonly IEnumerable<BaseCompetitionDataProvider> _competitionDataProviders;
        private readonly IEnumerable<BasePlayerDataProvider> _playerDataProviders;
        private readonly IEnumerable<BaseSchoolDataProvider> _schoolDataProviders;
        private readonly IEnumerable<BaseTournamentDataProvider> _tournamentDataProviders;
        private readonly IEnumerable<BaseClubDataProvider> _clubDataProviders;
        private readonly TournamentFactory _tournamentFactory;
        private readonly Faker<Competition> _competitionFaker;
        private readonly Faker<Team> _basicTeamFaker;
        private readonly Faker<Team> _detailedTeamFaker;
        private readonly Faker<Club> _clubFaker;
        private readonly Faker<MatchLocation> _matchLocationFaker;

        internal SeedDataGenerator(Randomiser randomiser, IBowlingFiguresCalculator bowlingFiguresCalculator,
            IPlayerIdentityFinder playerIdentityFinder, IMatchFinder matchFinder,
            CompetitionFactory competitionFactory, SeasonFactory seasonFactory, TeamFactory teamFactory, ClubFactory clubFactory,
            TournamentFactory tournamentFactory, MatchLocationFactory matchLocationFactory,
            PlayerFactory playerFactory, CommentFactory commentFactory,
            MatchFactory matchFactory, IEnumerable<BaseMatchDataProvider> matchDataProviders, IEnumerable<BaseCompetitionDataProvider> competitionDataProviders,
            IEnumerable<BasePlayerDataProvider> playerDataProviders, IEnumerable<BaseSchoolDataProvider> schoolDataProviders,
            IEnumerable<BaseTournamentDataProvider> tournamentDataProviders, IEnumerable<BaseClubDataProvider> clubDataProviders)
        {
            _randomiser = randomiser ?? throw new ArgumentNullException(nameof(randomiser));
            _bowlingFiguresCalculator = bowlingFiguresCalculator ?? throw new ArgumentNullException(nameof(bowlingFiguresCalculator));
            _playerIdentityFinder = playerIdentityFinder ?? throw new ArgumentNullException(nameof(playerIdentityFinder));
            _matchFinder = matchFinder ?? throw new ArgumentNullException(nameof(matchFinder));
            _competitionFactory = competitionFactory ?? throw new ArgumentNullException(nameof(competitionFactory));
            _seasonFactory = seasonFactory ?? throw new ArgumentNullException(nameof(seasonFactory));
            _teamFactory = teamFactory ?? throw new ArgumentNullException(nameof(teamFactory));
            _tournamentFactory = tournamentFactory ?? throw new ArgumentNullException(nameof(tournamentFactory));
            _matchLocationFactory = matchLocationFactory ?? throw new ArgumentNullException(nameof(matchLocationFactory));
            _playerFactory = playerFactory ?? throw new ArgumentNullException(nameof(playerFactory));
            _commentFactory = commentFactory ?? throw new ArgumentNullException(nameof(commentFactory));
            _competitionFaker = competitionFactory?.CreateFaker() ?? throw new ArgumentNullException(nameof(competitionFactory));
            _basicTeamFaker = teamFactory?.CreateBasicTeamFaker() ?? throw new ArgumentNullException(nameof(teamFactory));
            _detailedTeamFaker = teamFactory.CreateDetailedTeamFaker();
            _clubFaker = clubFactory?.CreateFaker() ?? throw new ArgumentNullException(nameof(clubFactory));
            _matchLocationFaker = matchLocationFactory?.CreateFaker() ?? throw new ArgumentNullException(nameof(matchLocationFactory));
            _matchFactory = matchFactory ?? throw new ArgumentNullException(nameof(matchFactory));
            _matchDataProviders = matchDataProviders ?? throw new ArgumentNullException(nameof(matchDataProviders));
            _competitionDataProviders = competitionDataProviders ?? throw new ArgumentNullException(nameof(competitionDataProviders));
            _playerDataProviders = playerDataProviders ?? throw new ArgumentNullException(nameof(playerDataProviders));
            _schoolDataProviders = schoolDataProviders ?? throw new ArgumentNullException(nameof(schoolDataProviders));
            _tournamentDataProviders = tournamentDataProviders ?? throw new ArgumentNullException(nameof(tournamentDataProviders));
            _clubDataProviders = clubDataProviders ?? throw new ArgumentNullException(nameof(clubDataProviders));
        }

        internal List<(Team team, List<PlayerIdentity> identities)> GenerateTeams()
        {
            // Create a pool of teams of 8 players
            var poolOfTeams = new List<(Team team, List<PlayerIdentity> identities)>();
            for (var i = 0; i < 5; i++)
            {
                var team = _randomiser.IsEven(i) ? _detailedTeamFaker.Generate() : _basicTeamFaker.Generate();
                poolOfTeams.Add((team, _playerFactory.CreatePlayerIdentityFaker(team).Generate(8)));
            }

            return poolOfTeams;
        }

        internal TestData GenerateTestData()
        {
            var testData = new TestData();
            var playerComparer = new PlayerEqualityComparer();

            var poolOfTeamsWithPlayers = GenerateTeams();

            // Create a pool of competitions
            for (var i = 0; i < 10; i++)
            {
                if (_randomiser.IsEven(i))
                {
                    testData.Competitions.Add(_competitionFactory.CreateCompetitionWithFullDetails());
                    var team1 = poolOfTeamsWithPlayers[_randomiser.PositiveIntegerLessThan(poolOfTeamsWithPlayers.Count)].team;
                    Team team2;
                    do
                    {
                        team2 = poolOfTeamsWithPlayers[_randomiser.PositiveIntegerLessThan(poolOfTeamsWithPlayers.Count)].team;
                    }
                    while (team2.TeamId == team1.TeamId);

                    var existingSummerSeasonsForCompetition = testData.Competitions[testData.Competitions.Count - 1].Seasons.Where(x => x.FromYear == x.UntilYear).Select(x => x.FromYear);
                    var newSummerSeason = DateTime.Now.Year - i;
                    while (existingSummerSeasonsForCompetition.Contains(newSummerSeason))
                    {
                        newSummerSeason--;
                    }

                    var season = _seasonFactory.CreateSeasonWithFullDetails(testData.Competitions[testData.Competitions.Count - 1], newSummerSeason, newSummerSeason, team1, team2);
                    testData.Competitions[testData.Competitions.Count - 1].Seasons.Add(season);
                }
                else
                {
                    testData.Competitions.Add(_competitionFaker.Generate());
                    testData.Competitions[testData.Competitions.Count - 1].Seasons.Add(_seasonFactory.CreateFaker(testData.Competitions[testData.Competitions.Count - 1], DateTime.Now.Year - i, DateTime.Now.Year - i).Generate());
                }
            }

            // Create a pool of match locations 
            for (var i = 0; i < 10; i++)
            {
                testData.MatchLocations.Add(_randomiser.IsEven(i) ? _matchLocationFactory.CreateMatchLocationWithFullDetails(_basicTeamFaker) : _matchLocationFaker.Generate());
            }
            testData.MatchLocations.AddRange(poolOfTeamsWithPlayers.SelectMany(x => x.team.MatchLocations).OfType<MatchLocation>());

            // Create match and tournament data
            testData.Matches = GenerateMatchData(testData, poolOfTeamsWithPlayers);

            testData.MatchInThePastWithMinimalDetails = FindMatchInThePastWithMinimalDetails(testData);

            testData.Tournaments.AddRange(testData.Matches.Where(x => x.Tournament != null && !testData.Tournaments.Select(t => t.TournamentId).Contains(x.Tournament.TournamentId)).Select(x => x.Tournament).OfType<Tournament>());

            testData.TournamentInThePastWithMinimalDetails = _tournamentFactory.CreateFaker().Generate();
            testData.Tournaments.Add(testData.TournamentInThePastWithMinimalDetails);

            testData.TournamentInTheFutureWithMinimalDetails = _tournamentFactory.CreateFaker().Generate();
            testData.TournamentInTheFutureWithMinimalDetails.StartTime = DateTimeOffset.UtcNow.AddMonths(1).UtcToUkTime();
            testData.Tournaments.Add(testData.TournamentInTheFutureWithMinimalDetails);

            var tournamentInTheFutureWithSeasons = _tournamentFactory.CreateTournamentInThePastWithFullDetailsExceptMatches();
            tournamentInTheFutureWithSeasons.StartTime = DateTimeOffset.UtcNow.AddMonths(1).UtcToUkTime();
            testData.Tournaments.Add(tournamentInTheFutureWithSeasons);

            testData.ClubWithMinimalDetails = _clubFaker.Generate();

            var clubsFromProviders = CreateTestDataFromClubProviders(testData);
            testData.ClubWithTeamsAndMatchLocation = clubsFromProviders.First(c => c.Teams.Any(t => t.MatchLocations.Any()));
            testData.MatchLocationForClub = testData.ClubWithTeamsAndMatchLocation.Teams.SelectMany(t => t.MatchLocations).OfType<MatchLocation>().First();

            var teamsInMatches = testData.Matches.SelectMany(x => x.Teams).Select(x => x.Team).OfType<Team>();
            var teamsInTournaments = testData.Tournaments.SelectMany(x => x.Teams).Select(x => x.Team).OfType<Team>();
            var teamsInSeasons = testData.Competitions.SelectMany(x => x.Seasons).SelectMany(x => x.Teams).Select(x => x.Team).OfType<Team>();
            var teamsAtMatchLocations = testData.MatchLocations.SelectMany(x => x.Teams);
            testData.Teams = poolOfTeamsWithPlayers.Select(x => x.team)
                            .Union(teamsInMatches)
                            .Union(teamsInTournaments)
                            .Union(teamsInSeasons)
                            .Union(teamsAtMatchLocations).Distinct(new TeamEqualityComparer()).ToList();
            testData.Teams.AddRange(clubsFromProviders.SelectMany(c => c.Teams));

            testData.Clubs.Add(testData.ClubWithMinimalDetails);
            testData.Clubs.AddRange(testData.Teams.Select(x => x.Club).OfType<Club>().Distinct(new ClubEqualityComparer()));

            // Get a minimal team
            testData.TeamWithMinimalDetails = FindTeamWithMinimalDetails(testData, teamsInMatches);

            foreach (var provider in _tournamentDataProviders)
            {
                foreach (var (tournament, matches) in provider.CreateTournaments(testData))
                {
                    if (!testData.Tournaments.Any(t => t.TournamentId == tournament.TournamentId))
                    {
                        testData.Tournaments.Add(tournament);
                    }

                    AddTeamsAndRelatedEntitiesToTestData(testData, tournament.Teams.Select(x => x.Team).OfType<Team>());

                    foreach (var match in matches)
                    {
                        AddMatchAndRelatedEntitiesToTestData(testData, match);
                    }
                }
            }

            testData.TournamentInThePastWithFullDetails = testData.Tournaments.First(t => t.History.Any());

            // Get a detailed team that's played a match. Resolved after the tournament providers have run, so that a
            // team playing in TournamentInThePastWithFullDetails is a candidate too, alongside teams that have only
            // played regular matches.
            testData.TeamWithFullDetails = testData.Teams.First(x =>
                        x.Club != null &&
                        x.MatchLocations.Any() &&
                        x.Seasons.Any() &&
                        teamsInMatches.Select(t => t.TeamId).Contains(x.TeamId)
            );
            if (testData.TeamWithFullDetails == null) { throw new InvalidOperationException($"{nameof(testData.TeamWithFullDetails)} not found"); }

            testData.MatchLocations.AddRange(testData.Matches.Select(m => m.MatchLocation)
                .Union(testData.Tournaments.Select(t => t.TournamentLocation))
                .Union(testData.Teams.SelectMany(x => x.MatchLocations))
                .OfType<MatchLocation>()
                .Distinct(new MatchLocationEqualityComparer())
                .Where(x => !testData.MatchLocations.Select(ml => ml.MatchLocationId).Contains(x.MatchLocationId)).ToList());

            testData.MatchLocationWithFullDetails = testData.MatchLocations.First(x => x.Teams.Any());
            testData.MatchLocationWithMinimalDetails = testData.MatchLocations.First(x => !x.Teams.Any());

            var competitionWithOneSeasonWithPointsRules = _competitionFaker.Generate();
            var pointsRulesSeasonTeam1 = poolOfTeamsWithPlayers[_randomiser.Between(0, poolOfTeamsWithPlayers.Count - 1)].team;
            Team pointsRulesSeasonTeam2;
            do
            {
                pointsRulesSeasonTeam2 = poolOfTeamsWithPlayers[_randomiser.Between(0, poolOfTeamsWithPlayers.Count - 1)].team;
            }
            while (pointsRulesSeasonTeam2.TeamId == pointsRulesSeasonTeam1.TeamId);
            competitionWithOneSeasonWithPointsRules.Seasons.Add(_seasonFactory.CreateSeasonWithFullDetails(competitionWithOneSeasonWithPointsRules,
                                                                                            2020, 2020,
                                                                                            pointsRulesSeasonTeam1,
                                                                                            pointsRulesSeasonTeam2));

            testData.Competitions = testData.Matches.Where(m => m.Season != null).Select(m => m.Season?.Competition)
                .Union(testData.Tournaments.Where(t => t.Seasons.Any()).SelectMany(t => t.Seasons.Select(s => s.Competition)))
                .Union(testData.Teams.SelectMany(x => x.Seasons).Select(x => x.Season?.Competition))
                .Union(new[] { _competitionFaker.Generate() })
                .Union(new[] { competitionWithOneSeasonWithPointsRules })
                .Union(CreateCompetitionsFromDataProviders(testData))
                .OfType<Competition>()
                .Distinct(new CompetitionEqualityComparer()).ToList();
            testData.CompetitionWithNoSeasons = testData.Competitions.First(x => !x.Seasons.Any());
            testData.CompetitionWithMinimalDetails = testData.Competitions.First(x =>
                !x.Seasons.Any() &&
                string.IsNullOrEmpty(x.Introduction) &&
                !x.UntilYear.HasValue &&
                string.IsNullOrEmpty(x.PublicContactDetails) && string.IsNullOrEmpty(x.PrivateContactDetails) &&
                string.IsNullOrEmpty(x.Facebook) && string.IsNullOrEmpty(x.Twitter) && string.IsNullOrEmpty(x.Instagram) && string.IsNullOrEmpty(x.YouTube) && string.IsNullOrEmpty(x.Website)
                );
            testData.CompetitionWithFullDetails = testData.Competitions.First(x => x.Seasons.Any());

            testData.Seasons = testData.Competitions.SelectMany(x => x.Seasons)
                .Union(testData.Teams.SelectMany(x => x.Seasons).Select(x => x.Season).OfType<Season>())
                .Distinct(new SeasonEqualityComparer()).ToList();

            testData.SeasonWithMinimalDetails = testData.Seasons.First(x => !x.Teams.Any()
                                                                         && !x.PointsAdjustments.Any()
                                                                         && !x.PointsRules.Any());
            testData.SeasonWithFullDetails = testData.Seasons.First(x => x.Teams.Any()
                                                                      && x.PointsRules.Any()
                                                                      && x.PointsAdjustments.Any());

            var playerIdentitiesInMatches = testData.Matches.SelectMany(_playerIdentityFinder.PlayerIdentitiesInMatch).Distinct(new PlayerIdentityEqualityComparer());
            testData.PlayerIdentities = playerIdentitiesInMatches.ToList();
            testData.Players = testData.PlayerIdentities.Select(x => x.Player).OfType<Player>().Distinct(playerComparer).ToList();

            foreach (var provider in _matchDataProviders)
            {
                foreach (var match in provider.CreateMatches(testData))
                {
                    AddMatchAndRelatedEntitiesToTestData(testData, match);
                }
            }

            testData.MatchInThePastWithFullDetails = FindMatchInThePastWithFullDetails(testData);

            testData.MatchInThePastWithFullDetailsAndTournament = FindMatchInThePastWithFullDetailsAndTournament(testData);

            testData.MatchInTheFutureWithMinimalDetails = FindMatchInTheFutureWithMinimalDetails(testData);

            testData.MatchListings.AddRange(testData.Matches.Where(x => x.Tournament == null).Select(x => x.ToMatchListing()).Union(testData.Tournaments.Select(x => x.ToMatchListing())));
            testData.TournamentMatchListings.AddRange(testData.Matches.Where(x => x.Tournament != null).Select(x => x.ToMatchListing()));

            // Get all batting records
            testData.PlayerInnings = testData.Matches.SelectMany(x => x.MatchInnings).SelectMany(x => x.PlayerInnings).ToList();

            CreateImmutableTestData(testData);

            BuildCollections(testData);

            testData.BowlerWithMultipleIdentities = CreateBowlerWithMultipleIdentities(testData, playerComparer);

            EnsureCyclicalRelationshipsArePopulated(testData);

            PopulateCalculatedProperties(testData);

            return testData;
        }

        private static Player? CreateBowlerWithMultipleIdentities(TestData testData, PlayerEqualityComparer playerComparer)
        {
            // Find any player who has multiple identities and bowled, and associate them to a member
            var player = testData.Matches
                .SelectMany(x => x.MatchInnings)
                .SelectMany(x => x.BowlingFigures)
                .Where(x => testData.PlayersWithMultipleIdentities.Contains(x.Bowler?.Player, playerComparer))
                .Select(x => x.Bowler?.Player)
                .First();
            player!.PlayerIdentities.Clear();
            player.PlayerIdentities.AddRange(testData.PlayerIdentities.Where(x => x.Player?.PlayerId == player.PlayerId));
            player.MemberKey = testData.AnyMemberNotLinkedToPlayer().Key;
            foreach (var identity in player.PlayerIdentities)
            {
                identity.LinkedBy = PlayerIdentityLinkedBy.Member;
            }
            return player;
        }

        private IEnumerable<Competition> CreateCompetitionsFromDataProviders(TestData testData)
        {
            var competitions = new List<Competition>();
            foreach (var provider in _competitionDataProviders)
            {
                var competitionsFromProvider = provider.CreateCompetitions(testData);
                foreach (var competition in competitionsFromProvider)
                {
                    competitions.Add(competition);
                }
            }

            return competitions;
        }

        /// <summary>
        /// Ensure that calculated properties are populated. Must not change any source data or relationships.
        /// </summary>
        /// <param name="testData"></param>
        private void PopulateCalculatedProperties(TestData testData)
        {
            // The following steps must happen after ALL scorecards and awards are finalised
            foreach (var identity in testData.PlayerIdentities)
            {
                var matchesPlayedByThisIdentity = _matchFinder.MatchesPlayedByPlayerIdentity(testData.Matches, identity.PlayerIdentityId!.Value);
                identity.TotalMatches = matchesPlayedByThisIdentity.Select(x => x.MatchId).Distinct().Count();
                if (identity.TotalMatches > 0)
                {
                    identity.FirstPlayed = matchesPlayedByThisIdentity.Min(x => x.StartTime);
                    identity.LastPlayed = matchesPlayedByThisIdentity.Max(x => x.StartTime);
                }
            }
        }

        /// <summary>
        /// Ensure that objects which have cyclical relationships have those relationships populated, in case test data providers did not populate them.
        /// </summary>
        /// <param name="testData"></param>
        private static void EnsureCyclicalRelationshipsArePopulated(TestData testData)
        {
            foreach (var identity in testData.PlayerIdentities)
            {
                // Ensure the cyclical relationship between players and identities is populated
                if (identity.Player is not null && !identity.Player.PlayerIdentities.Contains(identity)) { identity.Player.PlayerIdentities.Add(identity); }
            }
        }

        /// <summary>
        /// Build collections from finalised test data to avoid repeatedly querying to create the same collection.
        /// </summary>
        /// <param name="testData"></param>
        private static void BuildCollections(TestData testData)
        {
            // Add members created to support other objects
            var memberComparer = new MemberEqualityComparer();
            var membersFromPlayers = testData.Players.Where(p => p.MemberKey is not null).Select(p => new UmbracoMember { Key = p.MemberKey!.Value, Name = "No name" });
            var membersFromMatchComments = testData.Matches.SelectMany(x => x.Comments).Select(x => new UmbracoMember { Key = x.MemberKey, Name = x.MemberName ?? "No name" });
            var membersFromTournamentComments = testData.Tournaments.SelectMany(x => x.Comments).Select(x => new UmbracoMember { Key = x.MemberKey, Name = x.MemberName ?? "No name" });
            testData.Members = testData.Members
                              .Union(membersFromPlayers, memberComparer)
                              .Union(membersFromMatchComments, memberComparer)
                              .Union(membersFromTournamentComments, memberComparer)
                              .ToList();

            // Add player identities created to support other objects
            var playerIdentitiesFromPlayers = testData.Players.SelectMany(p => p.PlayerIdentities);
            testData.PlayerIdentities = testData.PlayerIdentities
                                       .Union(playerIdentitiesFromPlayers, new PlayerIdentityEqualityComparer())
                                       .ToList();

            // Add teams created to support other objects
            var teamsFromPlayers = testData.Players.SelectMany(p => p.PlayerIdentities).Where(pi => pi.Team is not null).Select(pi => pi.Team).OfType<Team>();
            var teamsFromSchools = testData.Schools.SelectMany(x => x.Teams);
            var teamsFromSeasons = testData.Seasons.SelectMany(x => x.Teams).Select(x => x.Team).OfType<Team>();

            var teamComparer = new TeamEqualityComparer();
            testData.Teams = testData.Teams
                            .Union(teamsFromPlayers, teamComparer)
                            .Union(teamsFromSchools, teamComparer)
                            .Union(teamsFromSeasons, teamComparer)
                            .ToList();

            // Add match locations created to support other objects
            testData.MatchLocations = testData.MatchLocations
                                     .Union(testData.Teams.SelectMany(x => x.MatchLocations), new MatchLocationEqualityComparer())
                                     .ToList();

            // Create collections from team data
            testData.TeamListings = CreateTeamListingsFromClubsAndTeams(testData);

            // Create collections from player data
            testData.PlayersWithMultipleIdentities = FindPlayersWithMultipleIdentities(testData);

            // Create collections from match data
            testData.Awards.AddRange(testData.Matches.SelectMany(m => m.Awards));
            testData.MatchInnings.AddRange(testData.Matches.SelectMany(x => x.MatchInnings));
            testData.BowlingFigures.AddRange(testData.MatchInnings.SelectMany(mi => mi.BowlingFigures));
        }

        private static List<TeamListing> CreateTeamListingsFromClubsAndTeams(TestData testData)
        {
            var teamListings = new List<TeamListing>();
            foreach (var team in testData.Teams.Where(t => t.Club == null ||
                                                                       t.Club.Teams.Count() == 1 ||
                                                                      (t.Club.Teams.Count(x => !x.UntilYear.HasValue) == 1 && !t.UntilYear.HasValue)))
            {
                teamListings.Add(team.ToTeamListing());
            }
            foreach (var club in testData.Clubs.Where(c => c.Teams.Count == 0 ||
                                                           c.Teams.Count(x => !x.UntilYear.HasValue) > 1))
            {
                teamListings.Add(club.ToTeamListing());
            }
            return teamListings;
        }

        /// <summary>
        /// Create test data from providers, for specific scenarios which must not be altered in case they are no longer valid.
        /// </summary>
        /// <param name="testData"></param>
        private void CreateImmutableTestData(TestData testData)
        {
            testData.Schools.AddRange(CreateTestDataFromSchoolProviders(testData));

            testData.Players.AddRange(CreateTestDataFromPlayerProviders(testData));
        }

        private List<Club> CreateTestDataFromClubProviders(TestData testData)
        {
            var clubs = new List<Club>();
            foreach (var provider in _clubDataProviders)
            {
                clubs.AddRange(provider.CreateClubs(testData));
            }
            return clubs;
        }

        private List<School> CreateTestDataFromSchoolProviders(TestData testData)
        {
            var schools = new List<School>();
            foreach (var provider in _schoolDataProviders)
            {
                schools.AddRange(provider.CreateSchools());
            }
            return schools;
        }

        private List<Player> CreateTestDataFromPlayerProviders(TestData testData)
        {
            var players = new List<Player>();
            foreach (var provider in _playerDataProviders)
            {
                players.AddRange(provider.CreatePlayers(testData));
            }
            return players;
        }

        private static List<Player> FindPlayersWithMultipleIdentities(TestData testData)
        {
            var results = new List<Player>();
            foreach (var identity in testData.PlayerIdentities)
            {
                if (testData.PlayerIdentities.Count(x => x.Player?.PlayerId == identity.Player?.PlayerId) > 1 &&
                    !results.Any(x => x.PlayerId == identity.Player?.PlayerId))
                {
                    results.Add(identity.Player!);
                }
            }

            foreach (var player in testData.Players.Where(p => p.PlayerIdentities.Count() > 1))
            {
                if (!results.Any(x => x.PlayerId == player.PlayerId))
                {
                    results.Add(player);
                }
            }
            return results;
        }

        private static Team FindTeamWithMinimalDetails(TestData testData, IEnumerable<Team> teamsInMatches)
        {
            return testData.Teams.FirstOrDefault(x =>
                                    string.IsNullOrEmpty(x.Introduction) &&
                                    !x.AgeRangeLower.HasValue && !x.AgeRangeUpper.HasValue &&
                                    string.IsNullOrEmpty(x.Facebook) && string.IsNullOrEmpty(x.Twitter) && string.IsNullOrEmpty(x.Instagram) && string.IsNullOrEmpty(x.YouTube) && string.IsNullOrEmpty(x.Website) &&
                                    string.IsNullOrEmpty(x.PlayingTimes) && string.IsNullOrEmpty(x.Cost) &&
                                    !x.UntilYear.HasValue &&
                                    string.IsNullOrEmpty(x.PublicContactDetails) && string.IsNullOrEmpty(x.PrivateContactDetails) &&
                                    x.Club == null &&
                                    !x.MatchLocations.Any() &&
                                    !x.Seasons.Any() &&
                                    !teamsInMatches.Any(t => t.TeamId == x.TeamId)
                                    )
                ?? throw new InvalidOperationException($"{nameof(FindTeamWithMinimalDetails)} did not find a team.");
        }

        /// <summary>
        /// Adds any of the given teams that aren't already part of <see cref="TestData"/>, along with their club (if
        /// any) and match locations - and, since a match location can itself come with its own teams, 
        /// recurses to pick those up too.
        /// </summary>
        private static void AddTeamsAndRelatedEntitiesToTestData(TestData testData, IEnumerable<Team> teams)
        {
            var newTeams = teams.Where(x => !testData.Teams.Any(t => t.TeamId == x.TeamId)).ToList();
            if (!newTeams.Any()) { return; }

            testData.Teams.AddRange(newTeams);

            var newClubs = newTeams.Select(x => x.Club).OfType<Club>().Where(x => !testData.Clubs.Any(c => c.ClubId == x.ClubId));
            testData.Clubs.AddRange(newClubs);

            var newMatchLocations = newTeams.SelectMany(x => x.MatchLocations).Where(x => !testData.MatchLocations.Any(ml => ml.MatchLocationId == x.MatchLocationId)).ToList();
            testData.MatchLocations.AddRange(newMatchLocations);

            AddTeamsAndRelatedEntitiesToTestData(testData, newMatchLocations.SelectMany(x => x.Teams));
        }

        /// <summary>
        /// Adds a match created by a provider to <see cref="TestData"/>, along with anything it references that isn't
        /// already part of the test data - new teams, player identities, players, a match location, a season and its
        /// competition, or a tournament.
        /// </summary>
        private void AddMatchAndRelatedEntitiesToTestData(TestData testData, Match match)
        {
            testData.Matches.Add(match);

            foreach (var matchInnings in match.MatchInnings)
            {
                matchInnings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(matchInnings);
            }

            AddTeamsAndRelatedEntitiesToTestData(testData, match.Teams.Select(x => x.Team).OfType<Team>());

            var newPlayerIdentities = _playerIdentityFinder.PlayerIdentitiesInMatch(match).Where(x => !testData.PlayerIdentities.Select(pi => pi.PlayerIdentityId).Contains(x.PlayerIdentityId)).ToList();
            if (newPlayerIdentities.Any()) { testData.PlayerIdentities.AddRange(newPlayerIdentities); }

            var newPlayers = newPlayerIdentities.Select(x => x.Player).Where(x => x != null && !testData.Players.Select(p => p.PlayerId).Contains(x.PlayerId)).OfType<Player>();
            if (newPlayers.Any()) { testData.Players.AddRange(newPlayers); }

            if (match.MatchLocation != null && !testData.MatchLocations.Any(ml => ml.MatchLocationId == match.MatchLocation.MatchLocationId))
            {
                testData.MatchLocations.Add(match.MatchLocation);
                AddTeamsAndRelatedEntitiesToTestData(testData, match.MatchLocation.Teams);
            }

            if (match.Season != null && !testData.Seasons.Any(s => s.SeasonId == match.Season.SeasonId))
            {
                testData.Seasons.Add(match.Season);
                if (match.Season.Competition != null && !testData.Competitions.Any(c => c.CompetitionId == match.Season.Competition.CompetitionId))
                {
                    testData.Competitions.Add(match.Season.Competition);
                }
            }

            if (match.Tournament != null && !testData.Tournaments.Any(t => t.TournamentId == match.Tournament.TournamentId))
            {
                testData.Tournaments.Add(match.Tournament);
            }
        }

        private static Match FindMatchInThePastWithFullDetailsAndTournament(TestData testData)
        {
            return testData.Matches.FirstOrDefault(x =>
                                x.StartTime < DateTime.UtcNow &&
                                x.Teams.Any() &&
                                x.Season != null && x.Season.Competition != null &&
                                x.MatchLocation != null &&
                                x.Tournament != null &&
                                x.Awards.Any() &&
                                x.Comments.Any() &&
                                x.MatchInnings.Any(i =>
                                        i.BattingTeam != null &&
                                        i.BowlingTeam != null &&
                                        i.PlayerInnings.Any() &&
                                        i.OverSets.Any() &&
                                        i.OversBowled.Any() &&
                                        i.BowlingFigures.Any()
                                    )
                                )
                ?? throw new InvalidOperationException($"{nameof(FindMatchInThePastWithFullDetailsAndTournament)} did not find a match.");
        }

        private static Match FindMatchInThePastWithFullDetails(TestData testData)
        {
            var match = testData.Matches.FirstOrDefault(x =>
                                x.StartTime < DateTime.UtcNow &&
                                x.Teams.Any() &&
                                x.PlayersPerTeam != null &&
                                x.Season != null && x.Season.Competition != null &&
                                x.MatchLocation != null &&
                                x.Tournament == null &&
                                x.Awards.Any() &&
                                x.Comments.Any() &&
                                x.MatchInnings.Any(i =>
                                        i.BattingTeam != null &&
                                        i.BowlingTeam != null &&
                                        i.PlayerInnings.Any() &&
                                        i.OverSets.Any() &&
                                        i.OversBowled.Any() &&
                                        i.BowlingFigures.Any()
                                    )
                                )
                ?? throw new InvalidOperationException($"{nameof(FindMatchInThePastWithFullDetails)} did not find a match.");

            match.Teams[0].Team!.UntilYear = 2020;
            match.History.AddRange(new[] { new AuditRecord {
                    Action = AuditAction.Create,
                    ActorName = nameof(SeedDataGenerator),
                    AuditDate = DateTimeOffset.UtcNow.AccurateToTheMinute().AddMonths(-1),
                    EntityUri = match.EntityUri
                }, new AuditRecord {
                    Action = AuditAction.Update,
                    ActorName = nameof(SeedDataGenerator),
                    AuditDate = DateTimeOffset.UtcNow.AccurateToTheMinute(),
                    EntityUri = match.EntityUri
                } });

            return match;
        }

        private static Match FindMatchInTheFutureWithMinimalDetails(TestData testData)
        {
            return testData.Matches.FirstOrDefault(x =>
                            x.StartTime > DateTime.UtcNow &&
                            !x.Teams.Any() &&
                            x.Season == null &&
                            x.MatchLocation == null &&
                            x.Tournament == null &&
                            !x.Awards.Any() &&
                            !x.Comments.Any() &&
                            !x.MatchInnings.Any(i =>
                               i.BattingTeam != null &&
                               i.BowlingTeam != null &&
                               i.PlayerInnings.Any() &&
                               i.OverSets.Any() &&
                               i.OversBowled.Any() &&
                               i.BowlingFigures.Any()
                            )
                        )
                ?? throw new InvalidOperationException($"{nameof(FindMatchInTheFutureWithMinimalDetails)} did not find a match.");
        }

        private static Match FindMatchInThePastWithMinimalDetails(TestData testData)
        {
            return testData.Matches.FirstOrDefault(x =>
                            x.StartTime < DateTime.UtcNow &&
                            !x.Teams.Any() &&
                            x.Season == null &&
                            x.MatchLocation == null &&
                            x.Tournament == null &&
                            !x.Awards.Any() &&
                            !x.Comments.Any() &&
                            !x.MatchInnings.Any(i =>
                               i.BattingTeam != null &&
                               i.BowlingTeam != null &&
                               i.PlayerInnings.Any() &&
                               i.OverSets.Any() &&
                               i.OversBowled.Any() &&
                               i.BowlingFigures.Any()
                            )
                        )
                ?? throw new InvalidOperationException($"{nameof(FindMatchInThePastWithFullDetails)} did not find a match.");
        }


        internal List<Match> GenerateMatchData(TestData testData, List<(Team team, List<PlayerIdentity> identities)> teamsWithIdentities)
        {
            // Randomly assign at least two players from each team a second identity - one on the same team, one on a different team.
            // This ensure we always have lots of teams with multiple identities for the same player for both scenarios.
            foreach (var (team, playerIdentities) in teamsWithIdentities)
            {
                // On the same team
                var player1 = playerIdentities[_randomiser.PositiveIntegerLessThan(playerIdentities.Count)];
                PlayerIdentity player2;
                do
                {
                    player2 = playerIdentities[_randomiser.PositiveIntegerLessThan(playerIdentities.Count)];
                } while (player1.PlayerIdentityId == player2.PlayerIdentityId);
                player2.Player = player1.Player;

                // On a different team
                var player3 = playerIdentities[_randomiser.PositiveIntegerLessThan(playerIdentities.Count)];
                (Team? targetTeam, List<PlayerIdentity>? targetIdentities) = (null, null);
                do
                {
                    (targetTeam, targetIdentities) = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
                } while (targetTeam.TeamId == team.TeamId);
                var player4 = targetIdentities[_randomiser.PositiveIntegerLessThan(targetIdentities.Count)];
                player4.Player = player3.Player;
            }

            var allIdentities = teamsWithIdentities.SelectMany(x => x.identities);
            foreach (var player in allIdentities.Select(x => x.Player).OfType<Player>())
            {
                player.PlayerIdentities = new PlayerIdentityList(allIdentities.Where(x => x.Player?.PlayerId == player.PlayerId));
            }

            // Create matches for them to play in, with scorecards
            var matches = new List<Match>();
            for (var i = 0; i < 40; i++)
            {
                var homeTeamBatsFirst = _randomiser.FiftyFiftyChance();

                var (teamA, teamAPlayers) = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
                (Team? teamB, List<PlayerIdentity>? teamBPlayers) = (null, null);
                do
                {
                    (teamB, teamBPlayers) = teamsWithIdentities[_randomiser.PositiveIntegerLessThan(teamsWithIdentities.Count)];
                }
                while (teamA.TeamId == teamB.TeamId);

                var match = _matchFactory.CreateMatchBetween(teamA, teamAPlayers, teamB, teamBPlayers, homeTeamBatsFirst, testData, nameof(GenerateMatchData) + "RandomMatches");
                if (_randomiser.FiftyFiftyChance())
                {
                    match.Comments = _commentFactory.CreateFaker().Generate(_randomiser.Between(1, 15));
                }

                match.MatchResultType = _randomiser.FiftyFiftyChance() ? new MatchResultType[] { MatchResultType.HomeWin, MatchResultType.AwayWin, MatchResultType.Tie }[_randomiser.PositiveIntegerLessThan(3)] : null;

                matches.Add(match);
            }

            // Ensure there's always an intra-club match to test
            matches.Add(_matchFactory.CreateMatchBetween(teamsWithIdentities[0].team, teamsWithIdentities[0].identities, teamsWithIdentities[0].team, teamsWithIdentities[0].identities, _randomiser.FiftyFiftyChance(), testData, nameof(GenerateMatchData) + "IntraClub"));

            matches.Add(_matchFactory.CreateMatchInThePast(false, testData, nameof(GenerateMatchData)));

            // Generate bowling figures for each innings
            foreach (var innings in matches.SelectMany(x => x.MatchInnings))
            {
                innings.BowlingFigures = _bowlingFiguresCalculator.CalculateBowlingFigures(innings);
            }

            return matches;
        }

    }
}