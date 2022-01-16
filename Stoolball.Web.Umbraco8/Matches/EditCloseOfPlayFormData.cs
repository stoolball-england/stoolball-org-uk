using System.Collections.Generic;
using Stoolball.Matches;

namespace Stoolball.Web.Matches
{
    public class EditCloseOfPlayFormData
    {
        public MatchResultType? MatchResultType { get; set; }
        public List<MatchAwardViewModel> Awards { get; internal set; } = new List<MatchAwardViewModel>();
    }
}