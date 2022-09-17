using System.Threading.Tasks;
using Stoolball.Web.Statistics.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Stoolball.Web.Statistics
{
    public interface IPlayerSummaryViewModelFactory
    {
        Task<PlayerSummaryViewModel> CreateViewModel(IPublishedContent currentPage, string path, string? queryString);
    }
}