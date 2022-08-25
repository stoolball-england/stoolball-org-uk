namespace Stoolball.Routing
{
    public interface IBestRouteSelector
    {
        string SelectBestRoute(string route1, string route2);
    }
}