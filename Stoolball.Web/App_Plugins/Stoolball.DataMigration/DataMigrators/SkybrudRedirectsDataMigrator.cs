using Stoolball.Umbraco.Data.Redirects;
using System;
using System.Threading.Tasks;
using Umbraco.Core.Logging;
using Umbraco.Web;

namespace Stoolball.Web.AppPlugins.Stoolball.DataMigration.DataMigrators
{
    /// <summary>
    /// Create redirects from the old website that are not related to a particular stoolball data entity
    /// </summary>
    public class SkybrudRedirectsDataMigrator : IRedirectsDataMigrator
    {
        private readonly IRedirectsRepository _redirectsRepository;
        private readonly ILogger _logger;
        public SkybrudRedirectsDataMigrator(IRedirectsRepository redirectsRepository, ILogger logger)
        {
            _redirectsRepository = redirectsRepository ?? throw new ArgumentNullException(nameof(redirectsRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task EnsureRedirects(IPublishedContentQuery publishedContentQuery)
        {
            if (publishedContentQuery is null)
            {
                throw new ArgumentNullException(nameof(publishedContentQuery));
            }

            try
            {
                await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/you").ConfigureAwait(false);

                var accountPage = publishedContentQuery.ContentSingleAtXPath("//myAccount");
                if (accountPage != null)
                {
                    await _redirectsRepository.InsertRedirect("/you/settings.php", accountPage.Id, accountPage.Key, new Uri(accountPage.Url, UriKind.Relative)).ConfigureAwait(false);
                    await _redirectsRepository.InsertRedirect("/you/essential.php", accountPage.Id, accountPage.Key, new Uri(accountPage.Url, UriKind.Relative)).ConfigureAwait(false);
                }

                var loginPage = publishedContentQuery.ContentSingleAtXPath("//loginMember");
                if (loginPage != null)
                {
                    await _redirectsRepository.InsertRedirect("/you", loginPage.Id, loginPage.Key, new Uri(loginPage.Url, UriKind.Relative)).ConfigureAwait(false);
                }

                var passwordResetPage = publishedContentQuery.ContentSingleAtXPath("//resetPassword");
                if (passwordResetPage != null)
                {
                    await _redirectsRepository.InsertRedirect("/you/request-password-reset", passwordResetPage.Id, passwordResetPage.Key, new Uri(passwordResetPage.Url, UriKind.Relative)).ConfigureAwait(false);
                }

                var createMemberPage = publishedContentQuery.ContentSingleAtXPath("//createMember");
                if (createMemberPage != null)
                {
                    await _redirectsRepository.InsertRedirect("/you/signup.php", createMemberPage.Id, createMemberPage.Key, new Uri(createMemberPage.Url, UriKind.Relative)).ConfigureAwait(false);
                }

                await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/play/manage/insurance").ConfigureAwait(false);
                await _redirectsRepository.InsertRedirect("/insurance", "/play/manage/insurance", null).ConfigureAwait(false);
                await _redirectsRepository.InsertRedirect("/manage/insurance", "/play/manage/insurance", null).ConfigureAwait(false);

                await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/teams/all").ConfigureAwait(false);
                await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/teams/ladies").ConfigureAwait(false);
                await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/teams/mixed").ConfigureAwait(false);
                await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/teams/junior").ConfigureAwait(false);
                await _redirectsRepository.DeleteRedirectsByDestinationPrefix("/teams/past").ConfigureAwait(false);

                await _redirectsRepository.InsertRedirect("/teams/all", "/teams", null).ConfigureAwait(false);
                await _redirectsRepository.InsertRedirect("/teams/ladies", "/teams?q=ladies", null).ConfigureAwait(false);
                await _redirectsRepository.InsertRedirect("/teams/mixed", "/teams?q=mixed", null).ConfigureAwait(false);
                await _redirectsRepository.InsertRedirect("/teams/junior", "/teams?q=junior", null).ConfigureAwait(false);
                await _redirectsRepository.InsertRedirect("/teams/past", "/teams", null).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _logger.Error<SkybrudRedirectsDataMigrator>(e);
                throw;
            }
        }
    }
}
