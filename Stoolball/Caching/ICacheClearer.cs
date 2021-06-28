using System.Threading.Tasks;

namespace Stoolball.Caching
{
    public interface ICacheClearer<T>
    {
        /// <summary>
        /// Clear important caches related to the <c>cacheable</c> item
        /// </summary>
        Task ClearCacheFor(T cacheable);
    }
}
