using System.Collections.Generic;
using Stoolball.Competitions;

namespace Stoolball.Web.Competitions
{
    public class SeasonListViewModel
    {
        public List<Competition> Competitions { get; internal set; } = new List<Competition>();
        public bool ShowCompetitionHeading { get; set; }
        public bool ShowSeasonFullName { get; set; }
    }
}