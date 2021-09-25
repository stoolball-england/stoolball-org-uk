namespace Stoolball
{
    public interface IStoolballEntityRouteParser
    {
        StoolballEntityType? ParseRoute(string route);
    }
}