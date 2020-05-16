using Stoolball.Audit;
using Stoolball.Teams;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Stoolball.Clubs
{
    public class Club : IAuditable
    {
        public Guid? ClubId { get; set; }

        [Required(ErrorMessage = "The club name field is required")]
        public string ClubName { get; set; }
        public List<Team> Teams { get; internal set; } = new List<Team>();
        public string Website { get; set; }
        public string Twitter { get; set; }

        [RegularExpression(@"^(|https?:\/\/(m.|www.|)facebook.com\/.+)", ErrorMessage = "Please enter a valid Facebook link")]
        public string Facebook { get; set; }
        public string Instagram { get; set; }

        [RegularExpression(@"^(|https?:\/\/(www.|)youtube.com\/.+)", ErrorMessage = "Please enter a valid YouTube link")]
        public string YouTube { get; set; }
        public bool ClubMark { get; set; }
        public string MemberGroupName { get; set; }
        public string ClubRoute { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/club/{ClubId}"); }
        }

        /// <summary>
        /// Gets a description of the club suitable for including in metadata or search results
        /// </summary>
        public string Description()
        {
            var description = new StringBuilder(ClubName).Append(" is a stoolball club");
            if (Teams?.Count > 0)
            {
                description.Append(" with ").Append(Teams.Count).Append(Teams.Count > 1 ? " teams: " : " team: ");
                for (var i = 0; i < Teams.Count; i++)
                {
                    description.Append(Teams[i].TeamName);
                    if (i < (Teams.Count - 2)) { description.Append(", "); };
                    if (i == (Teams.Count - 2)) { description.Append(" and "); };
                }
                description.Append(".");
            }
            else
            {
                description.Append(", but it does not have any active teams.");
            }
            return description.ToString();
        }
    }
}