using System;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Stoolball.Routing;

namespace Stoolball.Data.SqlServer
{
    public class SkybrudRedirectsRepository : IRedirectsRepository
    {
        public async Task DeleteRedirectsByDestinationPrefix(string destinationPrefix, IDbTransaction transaction)
        {
            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            await transaction.Connection.ExecuteAsync($@"DELETE FROM SkybrudRedirects WHERE DestinationUrl LIKE '{destinationPrefix}%'", null, transaction).ConfigureAwait(false);
        }

        public async Task InsertRedirect(string originalRoute, string revisedRoute, string routeSuffix, IDbTransaction transaction)
        {
            if (string.IsNullOrEmpty(originalRoute))
            {
                throw new ArgumentException($"'{nameof(originalRoute)}' cannot be null or empty", nameof(originalRoute));
            }

            if (string.IsNullOrEmpty(revisedRoute))
            {
                throw new ArgumentException($"'{nameof(revisedRoute)}' cannot be null or empty", nameof(revisedRoute));
            }

            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            await transaction.Connection.ExecuteAsync($@"INSERT INTO SkybrudRedirects 
							([Key], [RootId], [RootKey], [Url], [QueryString], [DestinationType], [DestinationId], [DestinationKey], 
							 [DestinationUrl], [Created], [Updated], [IsPermanent], [IsRegex], [ForwardQueryString])
							 VALUES (@Key, @RootId, @RootKey, @Url, @QueryString, @DestinationType, @DestinationId, @DestinationKey, @DestinationUrl, 
                             @Created, @Updated, @IsPermanent, @IsRegex, @ForwardQueryString)",
                                         new
                                         {
                                             Key = Guid.NewGuid().ToString(),
                                             RootId = 0,
                                             RootKey = "00000000-0000-0000-0000-000000000000",
                                             Url = "/" + originalRoute.TrimStart('/') + routeSuffix,
                                             QueryString = string.Empty,
                                             DestinationType = "url",
                                             DestinationId = 0,
                                             DestinationKey = "00000000-0000-0000-0000-000000000000",
                                             DestinationUrl = "/" + revisedRoute.TrimStart('/') + routeSuffix,
                                             Created = DateTime.UtcNow,
                                             Updated = DateTime.UtcNow,
                                             IsPermanent = true,
                                             IsRegex = false,
                                             ForwardQueryString = false
                                         },
                                        transaction).ConfigureAwait(false);
        }

        public async Task InsertRedirect(string originalRoute, int umbracoContentNodeId, Guid umbracoContentNodeKey, Uri umbracoContentNodeUrl, IDbTransaction transaction)
        {
            if (umbracoContentNodeUrl is null)
            {
                throw new ArgumentNullException(nameof(umbracoContentNodeUrl));
            }

            if (transaction is null)
            {
                throw new ArgumentNullException(nameof(transaction));
            }

            await transaction.Connection.ExecuteAsync($@"INSERT INTO SkybrudRedirects 
							([Key], [RootId], [RootKey], [Url], [QueryString], [DestinationType], [DestinationId], [DestinationKey], 
							 [DestinationUrl], [Created], [Updated], [IsPermanent], [IsRegex], [ForwardQueryString])
							 VALUES (@Key, @RootId, @RootKey, @Url, @QueryString, @DestinationType, @DestinationId, @DestinationKey, @DestinationUrl, 
                             @Created, @Updated, @IsPermanent, @IsRegex, @ForwardQueryString)",
                 new
                 {
                     Key = Guid.NewGuid().ToString(),
                     RootId = 0,
                     RootKey = "00000000-0000-0000-0000-000000000000",
                     Url = "/" + originalRoute?.TrimStart('/'),
                     QueryString = string.Empty,
                     DestinationType = "url",
                     DestinationId = umbracoContentNodeId,
                     DestinationKey = umbracoContentNodeKey,
                     DestinationUrl = umbracoContentNodeUrl.ToString(),
                     Created = DateTime.UtcNow,
                     Updated = DateTime.UtcNow,
                     IsPermanent = true,
                     IsRegex = false,
                     ForwardQueryString = false
                 },
                 transaction).ConfigureAwait(false);
        }
    }
}