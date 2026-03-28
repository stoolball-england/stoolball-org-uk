using Stoolball.Comments;

namespace Stoolball.Testing.Factories
{
    public class CommentFactory
    {
        public Faker<HtmlComment> CreateFaker(IEnumerable<UmbracoMember> members)
        {
            return new Faker<HtmlComment>()
                    .RuleFor(x => x.CommentId, () => Guid.NewGuid())
                    .RuleFor(x => x.CommentDate, (faker) => DateTimeOffset.UtcNow.AccurateToTheMinute().AddDays(faker.Random.Int(1, 1000) * -1))
                    .RuleFor(x => x.Comment, (faker) => $"<p>This is comment number <b>{faker.Random.Int()}</b>.</p>")
                    .Rules((faker, comment) =>
                    {
                        var member = faker.PickRandom(members);
                        comment.MemberKey = member.Key;
                        comment.MemberName = member.Name;
                    });
        }
    }
}
