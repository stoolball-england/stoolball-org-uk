using System.Collections.Generic;
using Stoolball.Schools;

namespace Stoolball.Testing.SchoolDataProviders
{
    internal abstract class BaseSchoolDataProvider
    {
        internal abstract IEnumerable<School> CreateSchools();
    }
}