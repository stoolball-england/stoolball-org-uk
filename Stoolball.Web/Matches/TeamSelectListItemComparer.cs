using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Stoolball.Web.Matches
{
    public class TeamSelectListItemComparer : IComparer<SelectListItem>
    {
        private readonly string? _homeTeamId;

        public TeamSelectListItemComparer(Guid? homeTeamId)
        {
            _homeTeamId = homeTeamId?.ToString();
        }

        public int Compare(SelectListItem? x, SelectListItem? y)
        {
            if (x is null)
            {
                throw new ArgumentNullException(nameof(x));
            }

            if (y is null)
            {
                throw new ArgumentNullException(nameof(y));
            }

            if (x.Value == _homeTeamId)
            {
                return -1;
            }
            else if (y.Value == _homeTeamId)
            {
                return 1;
            }

            return string.Compare(x.Text, y.Text, StringComparison.OrdinalIgnoreCase);
        }
    }
}
