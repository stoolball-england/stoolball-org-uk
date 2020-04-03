using System;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Redirects
{
    public interface IRedirectsRepository
    {
        Task DeleteRedirectsByDestinationPrefix(string destinationPrefix);
        Task InsertRedirect(string originalRoute, int umbracoContentNodeId, Guid umbracoContentNodeKey, Uri umbracoContentNodeUrl);
        Task InsertRedirect(string originalRoute, string revisedRoute, string routeSuffix);
    }
}