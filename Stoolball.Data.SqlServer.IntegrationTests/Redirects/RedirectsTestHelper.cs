using System.Data;
using Moq;
using Stoolball.Data.Abstractions;

namespace Stoolball.Data.SqlServer.IntegrationTests.Redirects
{
    internal static class RedirectsTestHelper
    {
        internal static void VerifyPlayerIsRedirected(this Mock<IRedirectsRepository> redirectsRepository, string originalRoute, string revisedRoute)
        {
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, null, It.IsAny<IDbTransaction>()), Times.Once);
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/batting", It.IsAny<IDbTransaction>()), Times.Once);
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/bowling", It.IsAny<IDbTransaction>()), Times.Once);
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/fielding", It.IsAny<IDbTransaction>()), Times.Once);
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/individual-scores", It.IsAny<IDbTransaction>()), Times.Once);
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/bowling-figures", It.IsAny<IDbTransaction>()), Times.Once);
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/catches", It.IsAny<IDbTransaction>()), Times.Once);
            redirectsRepository.Verify(x => x.InsertRedirect(originalRoute, revisedRoute, "/run-outs", It.IsAny<IDbTransaction>()), Times.Once);
        }
    }
}
