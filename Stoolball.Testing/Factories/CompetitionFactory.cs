namespace Stoolball.Testing.Factories
{
    public class CompetitionFactory(SeasonFactory _seasonFactory)
    {
        public Faker<Competition> CreateFaker()
        {
            return new Faker<Competition>()
                    .RuleFor(x => x.CompetitionId, () => Guid.NewGuid())
                    .RuleFor(x => x.CompetitionName, faker => $"{string.Join(' ', faker.Lorem.Words(3))} {faker.Random.ListItem(["League", "Association", "Friendlies", "Group"])}")
                    .RuleFor(x => x.PlayerType, faker => faker.Random.Enum<PlayerType>())
                    .RuleFor(x => x.MemberGroupKey, () => Guid.NewGuid())
                    .RuleFor(x => x.MemberGroupName, (faker, competition) => competition.CompetitionName + " owners")
                    .RuleFor(x => x.CompetitionRoute, (faker, competition) => $"/competition/{competition.CompetitionName.Kebaberize()}-{competition.CompetitionId}");
        }

        /// <summary>
        /// Creates a competition with three seasons covering a range of match types, for tests that need a competition with everything populated.
        /// </summary>
        public Competition CreateCompetitionWithFullDetails()
        {
            var competition = new Competition
            {
                CompetitionId = Guid.NewGuid(),
                CompetitionName = "Example league",
                PlayerType = PlayerType.JuniorMixed,
                Introduction = "Introduction to the competition",
                UntilYear = 2020,
                PublicContactDetails = "Public contact details",
                PrivateContactDetails = "Private contact details",
                Facebook = "https://facebook.com/example-league",
                Twitter = "@exampleleague",
                Instagram = "@examplephotos",
                YouTube = "https://youtube.com/exampleleague",
                Website = "https://example.org",
                CompetitionRoute = "/competitions/example-league-" + Guid.NewGuid(),
                MemberGroupKey = Guid.NewGuid(),
                MemberGroupName = "Example league owners",
            };
            competition.Seasons = new List<Season> {
                    _seasonFactory.CreateFaker(competition,2021,2021).Generate(),
                    _seasonFactory.CreateFaker(competition,2020,2021).Generate(),
                    _seasonFactory.CreateFaker(competition,2020,2020).Generate()
                };
            competition.Seasons[1].MatchTypes = [MatchType.LeagueMatch, MatchType.KnockoutMatch]; // matches a specific test in UpdateSeasonTests
            competition.Seasons[2].MatchTypes = [MatchType.LeagueMatch, MatchType.FriendlyMatch, MatchType.KnockoutMatch, MatchType.TrainingSession, MatchType.GroupMatch]; // every type

            return competition;
        }
    }
}