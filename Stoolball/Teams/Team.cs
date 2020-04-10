using Stoolball.Clubs;
using Stoolball.Schools;
using System;
using System.Text.RegularExpressions;

namespace Stoolball.Teams
{
    public class Team
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }

        /// <summary>
        /// Gets the version of the team's name used to match them for updates
        /// </summary>
        /// <returns></returns>
        public string GenerateComparableName()
        {
            return (TeamRoute + Regex.Replace(TeamNameAndPlayerType(), "[^A-Z0-9]", string.Empty, RegexOptions.IgnoreCase)).ToUpperInvariant();
        }

        /// <summary>
        /// Gets the name of the team and the type of players (if not stated in the name)
        /// </summary>
        /// <returns></returns>
        public string TeamNameAndPlayerType()
        {
            var name = TeamName;
            var type = PlayerType.ToString();
            if (!name.Replace("'", string.Empty).ToUpperInvariant().Contains(type.Replace("'", string.Empty).ToUpperInvariant()))
            {
                name += " (" + type + ")";
            }
            return name;
        }

        public Club Club { get; set; }
        public School School { get; set; }
        public TeamType TeamType { get; set; }
        public PlayerType PlayerType { get; set; }
        public string Introduction { get; set; }
        public int? AgeRangeLower { get; set; }
        public int? AgeRangeUpper { get; set; }
        public DateTimeOffset FromDate { get; set; }
        public DateTimeOffset? UntilDate { get; set; }
        public string Website { get; set; }
        public string Twitter { get; set; }
        public string Facebook { get; set; }
        public string Instagram { get; set; }
        public string YouTube { get; set; }
        public string PublicContactDetails { get; set; }
        public string PrivateContactDetails { get; set; }
        public string PlayingTimes { get; set; }
        public string Cost { get; set; }
        public int MemberGroupId { get; set; }
        public string TeamRoute { get; set; }
        public DateTimeOffset? DateCreated { get; set; }
        public DateTimeOffset? DateUpdated { get; set; }
        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/team/{TeamId}"); }
        }
    }
}
