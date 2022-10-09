using System;
using System.Collections.Generic;

namespace Stoolball.Competitions
{
    public class SeasonComparer : IComparer<Season>
    {
        public int Compare(Season? x, Season? y)
        {
            if (x is null)
            {
                throw new ArgumentNullException(nameof(x));
            }

            if (y is null)
            {
                throw new ArgumentNullException(nameof(y));
            }

            return string.Compare(x.SeasonFullName(), y.SeasonFullName(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
