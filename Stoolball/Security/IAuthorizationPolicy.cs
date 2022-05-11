using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Security
{
    public interface IAuthorizationPolicy<T>
    {
        /// <summary>
        /// Gets whether the current identity is authorized to carry out actions on the given item 
        /// </summary>
        /// <returns></returns>
        Task<Dictionary<AuthorizedAction, bool>> IsAuthorized(T item);

        /// <summary>
        /// Gets the group names authorized to edit the given item (not including the administrator group)
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        List<string> AuthorizedGroupNames(T item);

        /// <summary>
        /// Gets the members authorized to edit the given item (not including the current member)
        /// </summary>
        /// <returns></returns>
        Task<List<string>> AuthorizedMemberNames(T item);
    }
}