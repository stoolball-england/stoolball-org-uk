using System;

namespace Stoolball.Web.Routing
{
    public interface IStoolballRouteTypeMapper
    {
        Type MapRouteTypeToController(string unparsedRouteType);
    }
}