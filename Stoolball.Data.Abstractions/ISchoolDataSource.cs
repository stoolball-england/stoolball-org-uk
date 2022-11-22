using System.Collections.Generic;
using System.Threading.Tasks;
using Stoolball.Schools;

namespace Stoolball.Data.Abstractions
{
    /// <summary>
    /// Get school data from a data source
    /// </summary>
    public interface ISchoolDataSource
    {
        /// <summary>
        /// Gets the number of schools that match a query
        /// </summary>
        /// <returns></returns>
        Task<int> ReadTotalSchools(SchoolFilter filter);

        /// <summary>
        /// Gets a list of schools based on a query
        /// </summary>
        /// <returns>A list of <see cref="School"/> objects. An empty list if no schools are found.</returns>
        Task<List<School>> ReadSchools(SchoolFilter filter);
    }
}