using System.Collections.Generic;
using Umbraco.Cms.Core.Models;

namespace Stoolball.Web.Account
{
    /// <summary>
    /// Finds members by the value of one of their properties.
    /// </summary>
    public interface IMemberPropertySearch
    {
        /// <summary>
        /// Finds members whose property exactly matches a value, ignoring case.
        /// </summary>
        /// <param name="propertyAlias">The alias of the member property to search.</param>
        /// <param name="value">The value the property must have.</param>
        /// <returns>The matching members, or an empty collection if none match.</returns>
        IEnumerable<IMember> FindMembersByPropertyValue(string propertyAlias, string value);
    }
}
