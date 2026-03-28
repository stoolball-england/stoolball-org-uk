using System.Globalization;

namespace Stoolball.Testing
{
    public record UmbracoMember
    {
        public required Guid Key { get; init; }
        public required string Name { get; init; }
        public string Username() => Name.ToLower(CultureInfo.CurrentCulture).Replace(" ", ".") + "@example.org";
    }
}
