using System;
using System.Data;
using System.Threading.Tasks;

namespace Stoolball.Routing
{
    public interface IRedirectsRepository
    {
        Task DeleteRedirectsByDestinationPrefix(string destinationPrefix);
        Task InsertRedirect(string originalRoute, int umbracoContentNodeId, Guid umbracoContentNodeKey, Uri umbracoContentNodeUrl);
        Task InsertRedirect(string originalRoute, string revisedRoute, string routeSuffix);
        Task InsertRedirect(string originalRoute, string revisedRoute, string routeSuffix, IDbTransaction transaction);
    }
}