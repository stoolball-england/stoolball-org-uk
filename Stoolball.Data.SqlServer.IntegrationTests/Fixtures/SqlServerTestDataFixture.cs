using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Humanizer;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Schools;
using Stoolball.Statistics;
using Stoolball.Teams;
using Stoolball.Testing;

namespace Stoolball.Data.SqlServer.IntegrationTests.Fixtures
{
    public class SqlServerTestDataFixture : BaseSqlServerFixture
    {
        public TestData TestData { get; set; }

        public SqlServerTestDataFixture() : base("StoolballStatisticsDataSourceIntegrationTests")
        {
            // Populate seed data so that there's a consistent baseline for each test run
            var oversHelper = new OversHelper();
            var bowlingFiguresCalculator = new BowlingFiguresCalculator(oversHelper);
            var playerIdentityFinder = new PlayerIdentityFinder();
            var playerInMatchStatisticsBuilder = new PlayerInMatchStatisticsBuilder(playerIdentityFinder, oversHelper);
            var randomSeedDataGenerator = new SeedDataGenerator(oversHelper, bowlingFiguresCalculator, playerIdentityFinder);
            TestData = randomSeedDataGenerator.GenerateTestData();

            // Use Bogus to generate Schools data
            Randomizer.Seed = new Random(85437684);
            var schoolFaker = CreateSchoolFaker();
            TestData.Schools = schoolFaker.Generate(20);

            CreateSchoolTeamsForSchools(TestData.Schools, CreateTeamFaker, CreateMatchLocationFaker);

            TestData.Teams.AddRange(TestData.Schools.SelectMany(x => x.Teams));
            TestData.MatchLocations.AddRange(TestData.Schools.SelectMany(x => x.Teams).SelectMany(x => x.MatchLocations).Distinct(new MatchLocationEqualityComparer()));

            using (var connection = ConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();

                var repo = new SqlServerIntegrationTestsRepository(connection, playerInMatchStatisticsBuilder);
                repo.CreateUmbracoBaseRecords();
                repo.CreateTestData(TestData);
            }
        }

        private void CreateSchoolTeamsForSchools(List<School> schools, Func<Faker<Team>> teamFakerMaker, Func<Faker<MatchLocation>> locationFakerMaker)
        {
            var schoolTeamFaker = teamFakerMaker()
                .RuleFor(x => x.TeamType, faker => faker.Random.ListItem<TeamType>(new List<TeamType> { TeamType.SchoolAgeGroup, TeamType.SchoolClub, TeamType.SchoolOther }));
            var locationFaker = locationFakerMaker();

            for (var i = 3; i < schools.Count; i++)
            {
                // First 3 schools have no teams therefore the school should be inactive.
                // Then even indexes get one team, odd get multiple.
                TestData.Schools[i].Teams = schoolTeamFaker.Generate((i % 2) + 1);
                TestData.Schools[i].Teams.ForEach(t => t.School = TestData.Schools[i]);

                // Up to 7 they don't get a match location. Above that they do.
                // At index 9 the school with two teams gets the same location twice. 
                // At index 11 the school gets multiple teams with different locations AND multiple locations for a team.
                if (i == 9)
                {
                    // handle special case
                    var location = locationFaker.Generate(1).First();
                    TestData.Schools[i].Teams.ForEach(x => x.MatchLocations.Add(location));
                }
                else if (i == 11)
                {
                    // handle special case
                    TestData.Schools[i].Teams[0].MatchLocations.AddRange(locationFaker.Generate(2));
                    TestData.Schools[i].Teams[1].MatchLocations.Add(locationFaker.Generate(1).First());
                }
                else if (i > 7)
                {
                    // add a match location to each team
                    TestData.Schools[i].Teams.ForEach(x => x.MatchLocations.Add(locationFaker.Generate(1).First()));
                }

                // Up to 11, all teams are active.
                // Above 11, one team in multiple is inactive.
                // Above 15, both teams are inactive therefore the school should be inactive.
                if (i > 15 && TestData.Schools[i].Teams.Count > 1)
                {
                    TestData.Schools[i].Teams.ForEach(t => t.UntilYear = DateTimeOffset.UtcNow.AddYears(-1).Year);
                }
                else if (i > 11 && TestData.Schools[i].Teams.Count > 1)
                {
                    TestData.Schools[i].Teams[0].UntilYear = DateTimeOffset.UtcNow.AddYears(-2).Year;
                }
            }
        }

        private static Faker<MatchLocation> CreateMatchLocationFaker()
        {
            Func<string, int, string> maxLength = (string text, int maxLength) => text != null && text.Length > maxLength ? text.Substring(0, maxLength) : text;

            return new Faker<MatchLocation>()
                .RuleFor(x => x.MatchLocationId, () => Guid.NewGuid())
                .RuleFor(x => x.SecondaryAddressableObjectName, faker => maxLength(faker.Address.SecondaryAddress(), 100))
                .RuleFor(x => x.PrimaryAddressableObjectName, faker => maxLength(faker.Address.BuildingNumber(), 100))
                .RuleFor(x => x.StreetDescription, faker => maxLength(faker.Address.StreetName(), 100))
                .RuleFor(x => x.Locality, faker => maxLength(faker.Address.City(), 35))
                .RuleFor(x => x.Town, faker => maxLength(faker.Address.City(), 30))
                .RuleFor(x => x.AdministrativeArea, faker => maxLength(faker.Address.County(), 30))
                .RuleFor(x => x.Postcode, faker => maxLength(faker.Address.ZipCode(), 8))
                .RuleFor(x => x.MemberGroupKey, () => Guid.NewGuid())
                .RuleFor(x => x.MemberGroupName, (faker, location) => location.PrimaryAddressableObjectName + " " + location.StreetDescription + " owners")
                .RuleFor(x => x.MatchLocationRoute, (faker, location) => "/locations/" + (location.PrimaryAddressableObjectName + " " + location.StreetDescription).Kebaberize());
        }

        private static Faker<School> CreateSchoolFaker()
        {
            return new Faker<School>()
                            .RuleFor(x => x.SchoolId, () => Guid.NewGuid())
                            .RuleFor(x => x.SchoolName, faker => faker.Name.FullName() + " School")
                            .RuleFor(x => x.MemberGroupKey, () => Guid.NewGuid())
                            .RuleFor(x => x.MemberGroupName, (faker, school) => school.SchoolName + " owners")
                            .RuleFor(x => x.SchoolRoute, (faker, school) => "/schools/school/" + school.SchoolName.Kebaberize());
        }

        private static Faker<Team> CreateTeamFaker()
        {
            return new Faker<Team>()
                    .RuleFor(x => x.TeamId, () => Guid.NewGuid())
                    .RuleFor(x => x.TeamName, faker => faker.Address.City() + " " + faker.Random.ListItem<string>(new List<string> { "Tigers", "Bears", "Wolves", "Eagles", "Dolphins", "Stars", "Rockets" }))
                    .RuleFor(x => x.MemberGroupKey, () => Guid.NewGuid())
                    .RuleFor(x => x.MemberGroupName, (faker, team) => team.TeamName + " owners")
                    .RuleFor(x => x.TeamRoute, (faker, team) => "/teams/" + team.TeamName.Kebaberize());
        }
    }
}
