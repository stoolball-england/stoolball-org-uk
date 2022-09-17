namespace Stoolball.Routing
{
    public interface IRouteTokeniser
    {
        (string baseRoute, int? counter) TokeniseRoute(string route);
    }
}