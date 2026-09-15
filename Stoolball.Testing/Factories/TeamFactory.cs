namespace Stoolball.Testing.Factories
{
    public class TeamFactory(CompetitionFactory _competitionFactory, SeasonFactory _seasonFactory, MatchLocationFactory _matchLocationFactory)
    {
        public Faker<Team> CreateFaker()
        {
            return new Faker<Team>()
                    .RuleFor(x => x.TeamId, () => Guid.NewGuid())
                    .RuleFor(x => x.TeamName, faker => faker.Address.City() + " " + faker.Random.ListItem(["Tigers", "Bears", "Wolves", "Eagles", "Dolphins", "Stars", "Rockets", "Badgers", "Foxes", "Wildcats"]))
                    .RuleFor(x => x.MemberGroupKey, () => Guid.NewGuid())
                    .RuleFor(x => x.MemberGroupName, (faker, team) => team.TeamName + " owners")
                    .RuleFor(x => x.TeamRoute, (faker, team) => $"/teams/{team.TeamName.Kebaberize()}-{team.TeamId}");
        }

        /// <summary>
        /// Creates a team with match locations and seasons, for tests that need a team with everything populated.
        /// </summary>
        public Team CreateTeamWithFullDetails(string teamName)
        {
            var competition = _competitionFactory.CreateFaker().Generate();
            competition.PlayerType = PlayerType.Ladies; // Ensures there is always at least one Ladies competition
            var team = new Team
            {
                TeamId = Guid.NewGuid(),
                TeamName = teamName,
                TeamType = TeamType.Representative,
                TeamRoute = "/teams/" + teamName.Kebaberize() + "-" + Guid.NewGuid(),
                PlayerType = PlayerType.Ladies,
                Introduction = "Introduction to the team",
                AgeRangeLower = 11,
                AgeRangeUpper = 21,
                ClubMark = true,
                Facebook = "https://www.facebook.com/example-team",
                Twitter = "@teamtweets",
                Instagram = "@teamphotos",
                YouTube = "https://youtube.com/exampleteam",
                Website = "https://www.example.org",
                PlayingTimes = "Info on when this team plays",
                Cost = "Membership costs",
                UntilYear = 2019,
                PublicContactDetails = "Public contact details",
                PrivateContactDetails = "Private contact details",
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = teamName + " owners",
                MatchLocations = new List<MatchLocation> {
                    _matchLocationFactory.CreateFaker().Generate(),
                    _matchLocationFactory.CreateMatchLocationWithFullDetails(CreateFaker())
                },
                Seasons = new List<TeamInSeason> {
                    new TeamInSeason
                    {
                        Season = _seasonFactory.CreateFaker(competition, 2020, 2020).Generate()
                    },
                    new TeamInSeason
                    {
                        Season = _seasonFactory.CreateFaker(competition, 2019, 2019).Generate()
                    }
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
                ClubName = teamName + " Club",
                ClubRoute = "/clubs/" + teamName.Kebaberize() + "-" + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = teamName + " Club owners"
            };
            team.Club = club;
            club.Teams.Add(team);

            return team;
        }
    }
}
