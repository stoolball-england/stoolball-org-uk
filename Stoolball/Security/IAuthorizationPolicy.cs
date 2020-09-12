using System.Collections.Generic;

namespace Stoolball.Security
{
    public interface IAuthorizationPolicy<T>
    {
        /// <summary>
        /// Gets whether the current identity is authorized to carry out actions on the given item 
        /// </summary>
        /// <returns></returns>
        Dictionary<AuthorizedAction, bool> IsAuthorized(T item);
    }
}