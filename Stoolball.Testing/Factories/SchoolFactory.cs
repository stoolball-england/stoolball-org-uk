namespace Stoolball.Testing.Factories
{
    public class SchoolFactory
    {
        public Faker<School> CreateFaker()
        {
            return new Faker<School>()
                            .RuleFor(x => x.SchoolId, () => Guid.NewGuid())
                            .RuleFor(x => x.SchoolName, faker => faker.Name.FullName() + " School")
                            .RuleFor(x => x.MemberGroupKey, () => Guid.NewGuid())
                            .RuleFor(x => x.MemberGroupName, (faker, school) => school.SchoolName + " owners")
                            .RuleFor(x => x.SchoolRoute, (faker, school) => "/schools/school/" + school.SchoolName.Kebaberize());
        }
    }
}
