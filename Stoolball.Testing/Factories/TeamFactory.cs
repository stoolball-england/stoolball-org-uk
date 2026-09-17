namespace Stoolball.Testing.Factories
{
    public class TeamFactory(CompetitionFactory _competitionFactory, SeasonFactory _seasonFactory, MatchLocationFactory _matchLocationFactory)
    {
        public Faker<Team> CreateBasicTeamFaker()
        {
            return new Faker<Team>()
                    .RuleFor(x => x.TeamId, () => Guid.NewGuid())
                    .RuleFor(x => x.TeamName, faker => faker.Address.City() + " " + faker.Random.ListItem(["Tigers", "Bears", "Wolves", "Eagles", "Dolphins", "Stars", "Rockets", "Badgers", "Foxes", "Wildcats"]))
                    .RuleFor(x => x.MemberGroupKey, () => Guid.NewGuid())
                    .RuleFor(x => x.MemberGroupName, (faker, team) => team.TeamName + " owners")
                    .RuleFor(x => x.TeamRoute, (faker, team) => $"/teams/{team.TeamName.Kebaberize()}-{team.TeamId}");
        }

        /// <summary>
        /// Creates a faker for a team with match locations and seasons for a Ladies competition, for tests that need a team with everything populated.
        /// </summary>
        public Faker<Team> CreateDetailedTeamFaker()
        {
            return new Faker<Team>()
                    .RuleFor(x => x.TeamId, () => Guid.NewGuid())
                    .RuleFor(x => x.TeamName, faker => faker.Address.City() + " " + faker.Random.ListItem(["Tigers", "Bears", "Wolves", "Eagles", "Dolphins", "Stars", "Rockets", "Badgers", "Foxes", "Wildcats"]))
                    .RuleFor(x => x.MemberGroupKey, () => Guid.NewGuid())
                    .RuleFor(x => x.MemberGroupName, (faker, team) => team.TeamName + " owners")
                    .RuleFor(x => x.TeamRoute, (faker, team) => $"/teams/{team.TeamName.Kebaberize()}-{team.TeamId}")
                    .RuleFor(x => x.TeamType, faker => faker.Random.Enum<TeamType>())
                    .RuleFor(x => x.PlayerType, faker => faker.Random.Enum<PlayerType>())
                    .RuleFor(x => x.Introduction, faker => faker.Lorem.Sentence())
                    .RuleFor(x => x.AgeRangeLower, faker => faker.Random.Int(5, 15))
                    .RuleFor(x => x.AgeRangeUpper, (faker, team) => faker.Random.Int((team.AgeRangeLower ?? 5) + 1, 21))
                    .RuleFor(x => x.ClubMark, faker => faker.Random.Bool())
                    .RuleFor(x => x.Facebook, faker => $"https://www.facebook.com/{faker.Internet.UserName()}")
                    .RuleFor(x => x.Twitter, faker => "@" + faker.Internet.UserName())
                    .RuleFor(x => x.Instagram, faker => "@" + faker.Internet.UserName())
                    .RuleFor(x => x.YouTube, faker => $"https://youtube.com/{faker.Internet.UserName()}")
                    .RuleFor(x => x.Website, faker => faker.Internet.Url())
                    .RuleFor(x => x.PlayingTimes, faker => faker.Lorem.Sentence())
                    .RuleFor(x => x.Cost, faker => faker.Lorem.Sentence())
                    .RuleFor(x => x.UntilYear, faker => faker.Random.Int(2015, 2024))
                    .RuleFor(x => x.PublicContactDetails, faker => faker.Lorem.Sentence())
                    .RuleFor(x => x.PrivateContactDetails, faker => faker.Lorem.Sentence())
                    .FinishWith((faker, team) =>
                    {
                        var competition = _competitionFactory.CreateFaker().Generate();
                        competition.PlayerType = PlayerType.Ladies; // Ensures there is always at least one Ladies competition

                        team.MatchLocations = new List<MatchLocation> {
                            _matchLocationFactory.CreateFaker().Generate(),
                            _matchLocationFactory.CreateMatchLocationWithFullDetails(CreateBasicTeamFaker())
                        };
                        team.Seasons = new List<TeamInSeason> {
                            new TeamInSeason
                            {
                                Season = _seasonFactory.CreateFaker(competition, 2020, 2020).Generate()
                            },
                            new TeamInSeason
                            {
                                Season = _seasonFactory.CreateFaker(competition, 2019, 2019).Generate()
                            }
                        };
                        foreach (var matchLocation in team.MatchLocations)
                        {
                            matchLocation.Teams.Add(team);
                        }
                        foreach (var teamInSeason in team.Seasons)
                        {
                            teamInSeason.Team = team;
                            teamInSeason.Season!.Teams.Add(teamInSeason);
                        }
                        competition.Seasons.AddRange(team.Seasons.Select(x => x.Season)!);

                        // Built directly rather than via ClubFactory, which itself depends on TeamFactory.
                        var club = new Club
                        {
                            ClubId = Guid.NewGuid(),
                            ClubName = team.TeamName + " Club",
                            ClubRoute = "/clubs/" + team.TeamName.Kebaberize() + "-" + Guid.NewGuid(),
                            MemberGroupKey = Guid.NewGuid(),
                            MemberGroupName = team.TeamName + " Club owners"
                        };
                        team.Club = club;
                        club.Teams.Add(team);
                    });
        }
    }
}
