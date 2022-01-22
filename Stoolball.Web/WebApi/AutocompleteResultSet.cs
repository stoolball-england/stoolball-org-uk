using System;
using System.Collections.Generic;

namespace Stoolball.Web.WebApi
{
    public class AutocompleteResultSet
    {
        public IEnumerable<AutocompleteResult> suggestions { get; set; } = Array.Empty<AutocompleteResult>();
    }
}