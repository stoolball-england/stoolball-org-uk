using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Listings
{
    public interface IListingsModelBuilder<TModel, TFilter, TViewModel>
    {
        Task<TViewModel> BuildModel(
            Func<TViewModel> buildInitialState,
            Func<TFilter, Task<int>> totalListings,
            Func<TFilter, Task<List<TModel>>> listings,
            string pageTitle,
            Uri pageUrl,
            string? queryString);
    }
}