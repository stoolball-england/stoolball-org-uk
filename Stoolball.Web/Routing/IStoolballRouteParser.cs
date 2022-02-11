namespace Stoolball.Web.Routing
{
    public interface IStoolballRouteParser
    {
        StoolballRouteType? ParseRouteType(string route);
    }
}