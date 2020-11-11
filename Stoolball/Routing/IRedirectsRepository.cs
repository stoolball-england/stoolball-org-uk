using System;
using System.Data;
using System.Threading.Tasks;

namespace Stoolball.Routing
{
    public interface IRedirectsRepository
    {
        [Obsolete("Use the overload which requires an IDbTransaction")]
        Task DeleteRedirectsByDestinationPrefix(string destinationPrefix);
        Task DeleteRedirectsByDestinationPrefix(string destinationPrefix, IDbTransaction transaction);
        Task InsertRedirect(string originalRoute, int umbracoContentNodeId, Guid umbracoContentNodeKey, Uri umbracoContentNodeUrl);

        [Obsolete("Use the overload which requires an IDbTransaction")]
        Task InsertRedirect(string originalRoute, string revisedRoute, string routeSuffix);
        Task InsertRedirect(string originalRoute, string revisedRoute, string routeSuffix, IDbTransaction transaction);
    }
}