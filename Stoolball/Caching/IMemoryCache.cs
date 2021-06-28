using System;

namespace Stoolball.Caching
{
    public interface IClearableCache : IDisposable
    {
        /// <summary>
        /// Removes the object associated with the given key.
        /// </summary>
        void Remove(object key);
    }
}
