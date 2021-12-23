using System.Collections.Generic;
using Stoolball.Metadata;

namespace Stoolball.Listings
{
    public interface IListingsModel<TModel, TFilter> where TFilter : IListingsFilter
    {
        ViewMetadata Metadata { get; }
        TFilter Filter { get; set; }
        List<TModel> Listings { get; }
    }
}
