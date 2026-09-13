using System.Data;
using System.Threading.Tasks;

namespace Stoolball.Data.Abstractions
{
    public interface IRedirectsRepository
    {
        Task DeleteRedirectsByDestinationPrefix(string destinationPrefix, IDbConnection connection, IDbTransaction? transaction);

        Task InsertRedirect(string originalRoute, string revisedRoute, string? routeSuffix, IDbConnection connection, IDbTransaction? transaction);
    }
}