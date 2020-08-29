using Dapper;
using System;
using System.Data;
using System.Threading.Tasks;
using Umbraco.Core.Logging;

namespace Stoolball.Umbraco.Data.Redirects
{
    public class SkybrudRedirectsRepository : IRedirectsRepository
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly ILogger _logger;

        public SkybrudRedirectsRepository(IDatabaseConnectionFactory databaseConnectionFactory, ILogger logger)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task DeleteRedirectsByDestinationPrefix(string destinationPrefix)
        {
            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {

                        await transaction.Connection.ExecuteAsync($@"DELETE FROM SkybrudRedirects WHERE DestinationUrl LIKE '{destinationPrefix}%'", null, transaction).ConfigureAwait(false);

                        transaction.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error<SkybrudRedirectsRepository>(e);
                throw;
            }
        }

        public async Task InsertRedirect(string originalRoute, string revisedRoute, string routeSuffix)
        {
            if (string.IsNullOrEmpty(originalRoute))
            {
                throw new ArgumentException($"'{nameof(originalRoute)}' cannot be null or empty", nameof(originalRoute));
            }

            if (string.IsNullOrEmpty(revisedRoute))
            {
                throw new ArgumentException($"'{nameof(revisedRoute)}' cannot be null or empty", nameof(revisedRoute));
            }

            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {
                        await InsertRedirect(originalRoute, revisedRoute, routeSuffix, transaction).ConfigureAwait(false);
                        transaction.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error<SkybrudRedirectsRepository>(e);
                throw;
            }
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

            try
            {
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
                                                 Url = "/" + originalRoute?.TrimStart('/') + routeSuffix,
                                                 QueryString = string.Empty,
                                                 DestinationType = "url",
                                                 DestinationId = 0,
                                                 DestinationKey = "00000000-0000-0000-0000-000000000000",
                                                 DestinationUrl = "/" + revisedRoute?.TrimStart('/') + routeSuffix,
                                                 Created = DateTime.UtcNow,
                                                 Updated = DateTime.UtcNow,
                                                 IsPermanent = true,
                                                 IsRegex = false,
                                                 ForwardQueryString = false
                                             },
                                            transaction).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error<SkybrudRedirectsRepository>(e);
                throw;
            }
        }

        public async Task InsertRedirect(string originalRoute, int umbracoContentNodeId, Guid umbracoContentNodeKey, Uri umbracoContentNodeUrl)
        {
            if (umbracoContentNodeUrl is null)
            {
                throw new ArgumentNullException(nameof(umbracoContentNodeUrl));
            }

            try
            {
                using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
                {
                    connection.Open();
                    using (var transaction = connection.BeginTransaction())
                    {

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
                        transaction.Commit();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error<SkybrudRedirectsRepository>(e);
                throw;
            }
        }
    }
}