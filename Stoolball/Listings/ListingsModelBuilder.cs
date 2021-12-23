using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace Stoolball.Listings
{
    public class ListingsModelBuilder<TModel, TFilter, TViewModel> : IListingsModelBuilder<TModel, TFilter, TViewModel>
        where TViewModel : IListingsModel<TModel, TFilter>
        where TFilter : IListingsFilter
    {
        public async Task<TViewModel> BuildModel(
            Func<TViewModel> buildInitialState,
            Func<TFilter, Task<int>> totalListings,
            Func<TFilter, Task<List<TModel>>> listings,
            string pageTitle,
            Uri pageUrl,
            NameValueCollection queryParameters)
        {
            if (buildInitialState is null)
            {
                throw new ArgumentNullException(nameof(buildInitialState));
            }

            if (totalListings is null)
            {
                throw new ArgumentNullException(nameof(totalListings));
            }

            if (listings is null)
            {
                throw new ArgumentNullException(nameof(listings));
            }

            if (string.IsNullOrEmpty(pageTitle))
            {
                throw new ArgumentException($"'{nameof(pageTitle)}' cannot be null or empty.", nameof(pageTitle));
            }

            if (pageUrl is null)
            {
                throw new ArgumentNullException(nameof(pageUrl));
            }

            if (queryParameters is null)
            {
                throw new ArgumentNullException(nameof(queryParameters));
            }

            var model = buildInitialState();

            if (model == null || model.Filter == null)
            {
                throw new ArgumentException($"{ nameof(buildInitialState)} must return a non-null model with a non-null Filter property.");
            }

            model.Filter.Query = queryParameters["q"]?.Trim();

            _ = int.TryParse(queryParameters["page"], out var pageNumber);
            model.Filter.Paging.PageNumber = pageNumber > 0 ? pageNumber : 1;
            model.Filter.Paging.PageSize = Constants.Defaults.PageSize;
            model.Filter.Paging.PageUrl = pageUrl;
            model.Filter.Paging.Total = await totalListings(model.Filter).ConfigureAwait(false);
            model.Listings.Clear();
            model.Listings.AddRange(await listings(model.Filter).ConfigureAwait(false));

            model.Metadata.PageTitle = pageTitle;
            if (!string.IsNullOrEmpty(model.Filter.Query))
            {
                model.Metadata.PageTitle += $" matching '{model.Filter.Query}'";
            }

            return model;
        }
    }
}
