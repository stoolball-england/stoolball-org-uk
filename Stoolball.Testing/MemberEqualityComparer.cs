namespace Stoolball.Testing
{
    public class MemberEqualityComparer : EqualityComparer<UmbracoMember>
    {
        public override bool Equals(UmbracoMember? x, UmbracoMember? y)
        {
            return x?.Key == y?.Key;
        }

        public override int GetHashCode(UmbracoMember obj)
        {
            return base.GetHashCode();
        }
    }
}
