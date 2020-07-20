using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Stoolball.Web.Matches
{
    public class TeamComparer : IComparer<SelectListItem>
    {
        private readonly string _homeTeamId;

        public TeamComparer(Guid? homeTeamId)
        {
            _homeTeamId = homeTeamId?.ToString();
        }

        public int Compare(SelectListItem x, SelectListItem y)
        {
            if (x is null)
            {
                throw new System.ArgumentNullException(nameof(x));
            }

            if (y is null)
            {
                throw new System.ArgumentNullException(nameof(y));
            }

            if (x.Value == _homeTeamId)
            {
                return -1;
            }
            else if (y.Value == _homeTeamId)
            {
                return 1;
            }

            return string.Compare(x.Text, y.Text, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
