using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stoolball.Security
{
    [Obsolete]
    public interface ISynchronousAuthorizationPolicy<T>
    {
        /// <summary>
        /// Gets whether the current identity is authorized to carry out actions on the given item 
        /// </summary>
        /// <returns></returns>
        Dictionary<AuthorizedAction, bool> IsAuthorized(T item);
    }

    public interface IAuthorizationPolicy<T>
    {
        /// <summary>
        /// Gets whether the current identity is authorized to carry out actions on the given item 
        /// </summary>
        /// <returns></returns>
        Task<Dictionary<AuthorizedAction, bool>> IsAuthorized(T item);
    }
}