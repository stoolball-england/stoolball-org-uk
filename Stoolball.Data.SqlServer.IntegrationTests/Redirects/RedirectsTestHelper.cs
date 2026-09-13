using System.Data;
using Moq;
using Stoolball.Data.Abstractions;

namespace Stoolball.Data.SqlServer.IntegrationTests.Redirects
{
    internal static class RedirectsTestHelper
    {
        internal static void VerifyPlayerIsRedirected(this Mock<IRedirectsRepository> redirectsRepository, string originalRoute, string revisedRoute)
        {
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, null, It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()), Times.Once);
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/batting", It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()), Times.Once);
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/bowling", It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()), Times.Once);
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/fielding", It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()), Times.Once);
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/individual-scores", It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()), Times.Once);
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/bowling-figures", It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()), Times.Once);
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/catches", It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()), Times.Once);
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/run-outs", It.IsAny<IDbConnection>(), It.IsAny<IDbTransaction>()), Times.Once);
        }
    }
}
