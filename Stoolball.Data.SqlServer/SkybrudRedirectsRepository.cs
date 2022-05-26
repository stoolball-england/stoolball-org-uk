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

            await transaction.Connection.ExecuteAsync($@"SET IDENTITY_INSERT SkybrudRedirects ON;
                            INSERT INTO SkybrudRedirects 
							([Id], [Key], [RootId], [RootKey], [Url], [QueryString], [DestinationType], [DestinationId], [DestinationKey], 
							 [DestinationUrl], [Created], [Updated], [IsPermanent], [ForwardQueryString], [DestinationQuery], [DestinationFragment])
							 VALUES ((SELECT MAX(Id)+1 FROM SkybrudRedirects), @Key, @RootId, @RootKey, @Url, @QueryString, @DestinationType, @DestinationId, @DestinationKey, @DestinationUrl, 
                             @Created, @Updated, @IsPermanent, @ForwardQueryString, @DestinationQuery, @DestinationFragment);
                            SET IDENTITY_INSERT SkybrudRedirects OFF",
                                         new
                                         {
                                             Key = Guid.NewGuid().ToString(),
                                             RootId = 0,
                                             RootKey = "00000000-0000-0000-0000-000000000000",
                                             Url = "/" + originalRoute.TrimStart('/') + routeSuffix,
                                             QueryString = string.Empty,
                                             DestinationType = "Url",
                                             DestinationId = 0,
                                             DestinationKey = "00000000-0000-0000-0000-000000000000",
                                             DestinationUrl = "/" + revisedRoute.TrimStart('/') + routeSuffix,
                                             Created = DateTime.UtcNow,
                                             Updated = DateTime.UtcNow,
                                             IsPermanent = true,
                                             ForwardQueryString = true,
                                             DestinationQuery = string.Empty,
                                             DestinationFragment = string.Empty
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

            await transaction.Connection.ExecuteAsync($@"SET IDENTITY_INSERT SkybrudRedirects ON;
                            INSERT INTO SkybrudRedirects 
							([Id], [Key], [RootId], [RootKey], [Url], [QueryString], [DestinationType], [DestinationId], [DestinationKey], 
							 [DestinationUrl], [Created], [Updated], [IsPermanent], [ForwardQueryString], [DestinationQuery], [DestinationFragment])
							 VALUES ((SELECT MAX(Id)+1 FROM SkybrudRedirects), @Key, @RootId, @RootKey, @Url, @QueryString, @DestinationType, @DestinationId, @DestinationKey, @DestinationUrl, 
                             @Created, @Updated, @IsPermanent, @ForwardQueryString, @DestinationQuery, @DestinationFragment);
                            SET IDENTITY_INSERT SkybrudRedirects OFF",
                 new
                 {
                     Key = Guid.NewGuid().ToString(),
                     RootId = 0,
                     RootKey = "00000000-0000-0000-0000-000000000000",
                     Url = "/" + originalRoute?.TrimStart('/'),
                     QueryString = string.Empty,
                     DestinationType = "Content",
                     DestinationId = umbracoContentNodeId,
                     DestinationKey = umbracoContentNodeKey,
                     DestinationUrl = umbracoContentNodeUrl.ToString(),
                     Created = DateTime.UtcNow,
                     Updated = DateTime.UtcNow,
                     IsPermanent = true,
                     ForwardQueryString = false,
                     DestinationQuery = string.Empty,
                     DestinationFragment = string.Empty
                 },
                 transaction).ConfigureAwait(false);
        }
    }
}