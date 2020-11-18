using System;
using System.Threading.Tasks;
using Stoolball.Data.SqlServer;
using Stoolball.Routing;
using Umbraco.Core.Logging;
using Umbraco.Web;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    /// <summary>
    /// Create redirects from the old website that are not related to a particular stoolball data entity
    /// </summary>
    public class SkybrudRedirectsDataMigrator : IRedirectsDataMigrator
    {
        private readonly IDatabaseConnectionFactory _databaseConnectionFactory;
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly ILogger _logger;
        public SkybrudRedirectsDataMigrator(IDatabaseConnectionFactory databaseConnectionFactory, IRedirectsRepository redirectsRepository, ILogger logger)
        {
            _databaseConnectionFactory = databaseConnectionFactory ?? throw new ArgumentNullException(nameof(databaseConnectionFactory));
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task EnsureRedirects(IPublishedContentQuery publishedContentQuery)
        {
            if (publishedContentQuery is null)
            {
                throw new ArgumentNullException(nameof(publishedContentQuery));
            }

            using (var connection = _databaseConnectionFactory.CreateDatabaseConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/you", transaction).ConfigureAwait(false);

                    var accountPage = publishedContentQuery.ContentSingleAtXPath("//myAccount");
                    if (accountPage != null)
                    {
                        await _redirectsRepository.InsertRedirect("/you/settings.php", accountPage.Id, accountPage.Key, new Uri(accountPage.Url(), UriKind.Relative), transaction).ConfigureAwait(false);
                        await _redirectsRepository.InsertRedirect("/you/essential.php", accountPage.Id, accountPage.Key, new Uri(accountPage.Url(), UriKind.Relative), transaction).ConfigureAwait(false);
                    }

                    var loginPage = publishedContentQuery.ContentSingleAtXPath("//loginMember");
                    if (loginPage != null)
                    {
                        await _redirectsRepository.InsertRedirect("/you", loginPage.Id, loginPage.Key, new Uri(loginPage.Url(), UriKind.Relative), transaction).ConfigureAwait(false);
                    }

                    var passwordResetPage = publishedContentQuery.ContentSingleAtXPath("//resetPassword");
                    if (passwordResetPage != null)
                    {
                        await _redirectsRepository.InsertRedirect("/you/request-password-reset", passwordResetPage.Id, passwordResetPage.Key, new Uri(passwordResetPage.Url(), UriKind.Relative), transaction).ConfigureAwait(false);
                    }

                    var createMemberPage = publishedContentQuery.ContentSingleAtXPath("//createMember");
                    if (createMemberPage != null)
                    {
                        await _redirectsRepository.InsertRedirect("/you/signup.php", createMemberPage.Id, createMemberPage.Key, new Uri(createMemberPage.Url(), UriKind.Relative), transaction).ConfigureAwait(false);
                    }

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/play/manage/insurance", transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect("/insurance", "/play/manage/insurance", null, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect("/manage/insurance", "/play/manage/insurance", null, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/teams/all", transaction).ConfigureAwait(false);
                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/teams/ladies", transaction).ConfigureAwait(false);
                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/teams/mixed", transaction).ConfigureAwait(false);
                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/teams/junior", transaction).ConfigureAwait(false);
                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/teams/past", transaction).ConfigureAwait(false);

                    await _redirectsRepository.InsertRedirect("/teams/all", "/teams", null, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect("/teams/ladies", "/teams?q=ladies", null, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect("/teams/mixed", "/teams?q=mixed", null, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect("/teams/junior", "/teams?q=junior", null, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect("/teams/past", "/teams", null, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/about", transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect("/about/privacy-notice-match-results-and-comments", "/privacy/privacy-notice-match-results-and-comments", null, transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect("/about/privacy-notice-registering-with-stoolball-england", "/privacy/privacy-notice-registering-with-stoolball-england", null, transaction).ConfigureAwait(false);

                    await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/shop", transaction).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect("/shop", "/play/equipment/buy", null, transaction).ConfigureAwait(false);

                    transaction.Commit();
                }
            }
        }
    }
}
