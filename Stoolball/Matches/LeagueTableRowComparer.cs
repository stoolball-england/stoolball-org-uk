using System.Collections.Generic;

namespace Stoolball.Matches
{
    public class LeagueTableRowComparer : IComparer<ResultsTableRow>
    {
        public int Compare(ResultsTableRow x, ResultsTableRow y)
        {
            if (x is null)
            {
                throw new System.ArgumentNullException(nameof(x));
            }

            if (y is null)
            {
                throw new System.ArgumentNullException(nameof(y));
            }

            if (x.Points != y.Points)
            {
                return x.Points.CompareTo(y.Points) * -1;
            }
            else
            {
                return x.Played.CompareTo(y.Played) * -1;
            }
        }
    }
}
