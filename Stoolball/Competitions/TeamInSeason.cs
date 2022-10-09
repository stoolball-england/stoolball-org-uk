using System;
using System.ComponentModel.DataAnnotations;
using Stoolball.Teams;

namespace Stoolball.Competitions
{
    public class TeamInSeason
    {
        public Season? Season { get; set; }
        public Team? Team { get; set; }

        [Display(Name = "Date withdrew")]
        public DateTimeOffset? WithdrawnDate { get; set; }
    }
}