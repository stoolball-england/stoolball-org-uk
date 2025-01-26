using System;
using Bogus;
using Humanizer;
using Stoolball.MatchLocations;

namespace Stoolball.Testing.Fakers
{
    public class MatchLocationFakerFactory : IFakerFactory<MatchLocation>
    {
        public Faker<MatchLocation> Create()
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
    }
}
