namespace Stoolball.Testing.Factories
{
    public class MatchLocationFactory
    {
        public Faker<MatchLocation> CreateFaker()
        {
            Func<string?, int, string?> maxLength = (string? text, int max) => text != null && text.Length > max ? text.Substring(0, max) : text;

            return new Faker<MatchLocation>()
                .RuleFor(x => x.MatchLocationId, () => Guid.NewGuid())
                .RuleFor(x => x.SecondaryAddressableObjectName, faker => maxLength(faker.Address.SecondaryAddress(), 100))
                .RuleFor(x => x.PrimaryAddressableObjectName, faker => maxLength(faker.Address.BuildingNumber(), 100))
                .RuleFor(x => x.StreetDescription, faker => maxLength(faker.Address.StreetName(), 100))
                .RuleFor(x => x.Locality, faker => maxLength(faker.Address.City(), 35))
                .RuleFor(x => x.Town, faker => maxLength(faker.Address.City(), 30))
                .RuleFor(x => x.AdministrativeArea, faker => maxLength(faker.Address.County(), 30))
                .RuleFor(x => x.Postcode, faker => maxLength(faker.Address.ZipCode(), 8))
                .RuleFor(x => x.GeoPrecision, faker => faker.PickRandom<GeoPrecision>())
                .RuleFor(x => x.MemberGroupKey, () => Guid.NewGuid())
                .RuleFor(x => x.MemberGroupName, (faker, location) => location.PrimaryAddressableObjectName + " " + location.StreetDescription + " owners")
                .RuleFor(x => x.MatchLocationRoute, (faker, location) => "/locations/" + (location.PrimaryAddressableObjectName + " " + location.StreetDescription).Kebaberize());
        }

        /// <summary>
        /// Creates a match location with a mix of active, inactive and transient teams, to test how teams at a location are filtered and sorted.
        /// </summary>
        /// <param name="teamFaker">The faker used to generate the teams at the match location.</param>
        public MatchLocation CreateMatchLocationWithFullDetails(Faker<Team> teamFaker)
        {
            var activeTeam = teamFaker.Generate();
            activeTeam.TeamName = "Team active";
            var anotherActiveTeam = teamFaker.Generate();
            anotherActiveTeam.TeamName = "Team that plays";
            var transientTeam = teamFaker.Generate();
            transientTeam.TeamName = "Transient team";
            transientTeam.TeamType = TeamType.Transient;
            var inactiveTeam = teamFaker.Generate();
            inactiveTeam.TeamName = "Inactive but alphabetically first";
            inactiveTeam.UntilYear = 2019;

            var matchLocation = CreateFaker().Generate();
            matchLocation.Teams = [inactiveTeam, activeTeam, transientTeam, anotherActiveTeam];

            activeTeam.MatchLocations.Add(matchLocation);
            anotherActiveTeam.MatchLocations.Add(matchLocation);
            transientTeam.MatchLocations.Add(matchLocation);
            inactiveTeam.MatchLocations.Add(matchLocation);

            return matchLocation;
        }
    }
}
