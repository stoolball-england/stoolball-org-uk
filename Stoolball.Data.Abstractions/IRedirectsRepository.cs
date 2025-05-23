﻿using System.Data;
using System.Threading.Tasks;

namespace Stoolball.Data.Abstractions
{
    public interface IRedirectsRepository
    {
        Task DeleteRedirectsByDestinationPrefix(string destinationPrefix, IDbTransaction transaction);

        Task InsertRedirect(string originalRoute, string revisedRoute, string? routeSuffix, IDbTransaction transaction);
    }
}