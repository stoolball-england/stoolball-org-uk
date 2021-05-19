using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Comments
{
    /// <summary>
    /// Get comments on a stoolball entity from a data source
    /// </summary>
    public interface ICommentsDataSource<T>
    {
        /// <summary>
        /// Gets the number of comments on an entity
        /// </summary>
        /// <returns></returns>
        Task<int> ReadTotalComments(Guid entityId);

        /// <summary>
        /// Gets the comments on an entity, with the newest first
        /// </summary>
        Task<List<HtmlComment>> ReadComments(Guid entityId);
    }
}