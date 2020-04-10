using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;

namespace Stoolball.Umbraco.Data.Redirects
{
	public class SkybrudRedirectsRepository : IRedirectsRepository
	{
		private readonly IScopeProvider _scopeProvider;
		private readonly ILogger _logger;

		public SkybrudRedirectsRepository(IScopeProvider scopeProvider, ILogger logger)
		{
			_scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task DeleteRedirectsByDestinationPrefix(string destinationPrefix)
		{
			try
			{
				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;

					await database.ExecuteAsync($@"DELETE FROM SkybrudRedirects WHERE DestinationUrl LIKE '{destinationPrefix}%'").ConfigureAwait(false);

					scope.Complete();
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
			try
			{
				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;

					await database.ExecuteAsync($@"INSERT INTO SkybrudRedirects 
							([Key], [RootId], [RootKey], [Url], [QueryString], [DestinationType], [DestinationId], [DestinationKey], 
							 [DestinationUrl], [Created], [Updated], [IsPermanent], [IsRegex], [ForwardQueryString])
							 VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13)",
								 Guid.NewGuid().ToString(),
								 0,
								 "00000000-0000-0000-0000-000000000000",
								 "/" + originalRoute?.TrimStart('/') + routeSuffix,
								 string.Empty,
								 "url",
								 0,
								 "00000000-0000-0000-0000-000000000000",
								 "/" + revisedRoute?.TrimStart('/') + routeSuffix,
								 DateTime.UtcNow,
								 DateTime.UtcNow,
								 true,
								 false,
								 false
								 ).ConfigureAwait(false);
					scope.Complete();
				}
			}
			catch (Exception e)
			{
				_logger.Error<SkybrudRedirectsRepository>(e);
				throw;
			}
		}

		public async Task InsertRedirect(string originalRoute, int umbracoContentNodeId, Guid umbracoContentNodeKey, Uri umbracoContentNodeUrl)
		{
			try
			{
				using (var scope = _scopeProvider.CreateScope())
				{
					var database = scope.Database;

					await database.ExecuteAsync($@"INSERT INTO SkybrudRedirects 
							([Key], [RootId], [RootKey], [Url], [QueryString], [DestinationType], [DestinationId], [DestinationKey], 
							 [DestinationUrl], [Created], [Updated], [IsPermanent], [IsRegex], [ForwardQueryString])
							 VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13)",
									 Guid.NewGuid().ToString(),
									 0,
									 "00000000-0000-0000-0000-000000000000",
									 "/" + originalRoute?.TrimStart('/'),
									 string.Empty,
									 "url",
									 umbracoContentNodeId,
									 umbracoContentNodeKey,
									 umbracoContentNodeUrl,
									 DateTime.UtcNow,
									 DateTime.UtcNow,
									 true,
									 false,
									 false).ConfigureAwait(false);
					scope.Complete();
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