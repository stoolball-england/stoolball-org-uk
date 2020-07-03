using System;
using System.Threading.Tasks;

namespace Stoolball.Umbraco.Data.Matches
{
    /// <summary>
    /// Get stoolball match comments from a data source
    /// </summary>
    public interface IMatchCommentsDataSource
    {
        /// <summary>
        /// Gets the number of comments on a match
        /// </summary>
        /// <returns></returns>
        Task<int> ReadTotalComments(Guid matchId);
    }
}