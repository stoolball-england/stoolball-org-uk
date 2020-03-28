using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Core.Scoping;
using Umbraco.Web;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
	/// <summary>
	/// Create redirects from the old website that are not related to a particular stoolball data entity
	/// </summary>
	public class SkybrudRedirectsDataMigrator : IRedirectsDataMigrator
	{
		private readonly IScopeProvider _scopeProvider;
		private readonly ILogger _logger;
		public SkybrudRedirectsDataMigrator(IScopeProvider scopeProvider, ILogger logger)
		{
			_scopeProvider = scopeProvider ?? throw new ArgumentNullException(nameof(scopeProvider));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		public async Task EnsureRedirects(IPublishedContentQuery publishedContentQuery)
		{
			if (publishedContentQuery is null)
			{
				throw new ArgumentNullException(nameof(publishedContentQuery));
			}

			using (var scope = _scopeProvider.CreateScope())
			{
				var database = scope.Database;
				try
				{
					using (var transaction = database.GetTransaction())
					{
						await database.ExecuteAsync($@"DELETE FROM SkybrudRedirects WHERE Url LIKE '/you%'").ConfigureAwait(false);

						const string INSERT_SQL = @"INSERT INTO SkybrudRedirects 
							([Key], [RootId], [RootKey], [Url], [QueryString], [DestinationType], [DestinationId], [DestinationKey], 
							 [DestinationUrl], [Created], [Updated], [IsPermanent], [IsRegex], [ForwardQueryString])
							 VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10, @11, @12, @13)";

						var accountPage = publishedContentQuery.ContentSingleAtXPath("//myAccount");
						if (accountPage != null)
						{
							await database.ExecuteAsync(INSERT_SQL,
											 Guid.NewGuid().ToString(),
											 0,
											 "00000000-0000-0000-0000-000000000000",
											 "/you/settings.php",
											 string.Empty,
											 "content",
											 accountPage.Id,
											 accountPage.Key,
											 accountPage.Url,
											 DateTime.UtcNow,
											 DateTime.UtcNow,
											 true,
											 false,
											 false
											 ).ConfigureAwait(false);


							await database.ExecuteAsync(INSERT_SQL,
											 Guid.NewGuid().ToString(),
											 0,
											 "00000000-0000-0000-0000-000000000000",
											 "/you/essential.php",
											 string.Empty,
											 "content",
											 accountPage.Id,
											 accountPage.Key,
											 accountPage.Url,
											 DateTime.UtcNow,
											 DateTime.UtcNow,
											 true,
											 false,
											 false
											 ).ConfigureAwait(false);
						}

						var loginPage = publishedContentQuery.ContentSingleAtXPath("//loginMember");
						if (loginPage != null)
						{
							await database.ExecuteAsync(INSERT_SQL,
											 Guid.NewGuid().ToString(),
											 0,
											 "00000000-0000-0000-0000-000000000000",
											 "/you",
											 string.Empty,
											 "content",
											 loginPage.Id,
											 loginPage.Key,
											 loginPage.Url,
											 DateTime.UtcNow,
											 DateTime.UtcNow,
											 true,
											 false,
											 false
											 ).ConfigureAwait(false);
						}

						var passwordResetPage = publishedContentQuery.ContentSingleAtXPath("//resetPassword");
						if (passwordResetPage != null)
						{
							await database.ExecuteAsync(INSERT_SQL,
											 Guid.NewGuid().ToString(),
											 0,
											 "00000000-0000-0000-0000-000000000000",
											 "/you/request-password-reset",
											 string.Empty,
											 "content",
											 passwordResetPage.Id,
											 passwordResetPage.Key,
											 passwordResetPage.Url,
											 DateTime.UtcNow,
											 DateTime.UtcNow,
											 true,
											 false,
											 false
											 ).ConfigureAwait(false);
						}

						var createMemberPage = publishedContentQuery.ContentSingleAtXPath("//createMember");
						if (createMemberPage != null)
						{
							await database.ExecuteAsync(INSERT_SQL,
											 Guid.NewGuid().ToString(),
											 0,
											 "00000000-0000-0000-0000-000000000000",
											 "/you/signup.php",
											 string.Empty,
											 "content",
											 createMemberPage.Id,
											 createMemberPage.Key,
											 createMemberPage.Url,
											 DateTime.UtcNow,
											 DateTime.UtcNow,
											 true,
											 false,
											 false
											 ).ConfigureAwait(false);
						}

						transaction.Complete();
					}
				}
				catch (Exception e)
				{
					_logger.Error<SkybrudRedirectsDataMigrator>(e);
					throw;
				}
				scope.Complete();
			}
		}
	}
}