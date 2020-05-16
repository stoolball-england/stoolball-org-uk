namespace Stoolball.Routing
{
    public interface IRouteGenerator
    {
        string GenerateRoute(string prefix, string name);
        string IncrementRoute(string route);
    }
}