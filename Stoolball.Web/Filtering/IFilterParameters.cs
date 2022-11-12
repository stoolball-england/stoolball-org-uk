using System;

namespace Stoolball.Web.Filtering
{
    public interface IFilterParameters
    {
        DateTimeOffset? from { get; }
        string? team { get; }
        DateTimeOffset? to { get; }
    }
}