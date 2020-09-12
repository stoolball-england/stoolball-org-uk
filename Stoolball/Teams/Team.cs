using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Humanizer;
using Stoolball.Audit;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.MatchLocations;
using Stoolball.Schools;

namespace Stoolball.Teams
{
    public class Team : IAuditable
    {
        public Guid? TeamId { get; set; }

        [Display(Name = "Team name")]
        [Required]
        public string TeamName { get; set; }

        /// <summary>
        /// Gets the version of the team's name used to match them for updates
        /// </summary>
        /// <returns></returns>
        public string ComparableName()
        {
            return (TeamRoute + Regex.Replace(TeamNameAndPlayerType(), "[^A-Z0-9]", string.Empty, RegexOptions.IgnoreCase)).ToUpperInvariant();
        }

        /// <summary>
        /// Gets the name of the team and the type of players (if not stated in the name)
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="TeamName"/> is <c>null</c> or an empty string</exception>
        public string TeamNameAndPlayerType()
        {
            if (string.IsNullOrEmpty(TeamName))
            {
                throw new InvalidOperationException($"{nameof(TeamName)} is null or an empty string");
            }

            var name = TeamName;
            var type = PlayerType.ToString().Humanize(LetterCasing.Sentence);
            if (!name.Replace("'", string.Empty).ToUpperInvariant().Contains(type.Replace("'", string.Empty).ToUpperInvariant()))
            {
                name += " (" + type + ")";
            }
            return name;
        }


        /// <summary>
        /// Gets the name of the team, its location and the type of players (if not stated in the name)
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if <see cref="TeamName"/> is <c>null</c> or an empty string</exception>
        public string TeamNameLocationAndPlayerType()
        {
            if (string.IsNullOrEmpty(TeamName))
            {
                throw new InvalidOperationException($"{nameof(TeamName)} is null or an empty string");
            }

            var name = TeamName;

            var location = MatchLocations.FirstOrDefault()?.LocalityOrTown();
            if (!string.IsNullOrEmpty(location) &&
                !name.Replace("'", string.Empty).ToUpperInvariant().Contains(location.Replace("'", string.Empty).ToUpperInvariant()))
            {
                name += ", " + location;
            }

            var type = PlayerType.ToString().Humanize(LetterCasing.Sentence);
            if (!name.Replace("'", string.Empty).ToUpperInvariant().Contains(type.Replace("'", string.Empty).ToUpperInvariant()))
            {
                name += " (" + type + ")";
            }
            return name;
        }

        public Club Club { get; set; }
        public School School { get; set; }

        public List<MatchLocation> MatchLocations { get; internal set; } = new List<MatchLocation>();
        public List<TeamInSeason> Seasons { get; internal set; } = new List<TeamInSeason>();

        [Required]
        [Display(Name = "Type of team")]
        public TeamType TeamType { get; set; }

        [Required]
        [Display(Name = "Player type")]
        public PlayerType PlayerType { get; set; }

        public List<Player> Players { get; internal set; } = new List<Player>();

        public string Introduction { get; set; }

        [Display(Name = "Minimum age for players")]
        public int? AgeRangeLower { get; set; }

        [Display(Name = "Maximum age for players")]
        public int? AgeRangeUpper { get; set; }

        [Display(Name = "What year was this team formed?")]
        public int? FromYear { get; set; }

        [Display(Name = "If no longer active, when was the last year they played?")]
        public int? UntilYear { get; set; }

        [Display(Name = "Club or team website")]
        public string Website { get; set; }

        [Display(Name = "Twitter account")]
        public string Twitter { get; set; }

        [Display(Name = "Facebook page or group")]
        [RegularExpression(@"^((https?:\/\/)?(m.|www.|)facebook.com\/.+|)", ErrorMessage = "Please enter a valid Facebook link")]
        public string Facebook { get; set; }

        [Display(Name = "Instagram account")]
        public string Instagram { get; set; }

        [Display(Name = "YouTube channel")]
        [RegularExpression(@"^((https?:\/\/)?(www.|)youtube.com\/.+|)", ErrorMessage = "Please enter a valid YouTube link")]
        public string YouTube { get; set; }

        [Display(Name = "Contact details for the public")]
        public string PublicContactDetails { get; set; }

        [Display(Name = "Contact details for Stoolball England")]
        public string PrivateContactDetails { get; set; }

        [Display(Name = "Which days of the week do you play, and at what time?")]
        public string PlayingTimes { get; set; }

        [Display(Name = "Cost to play")]
        public string Cost { get; set; }
        public int MemberGroupId { get; set; }
        public string MemberGroupName { get; set; }
        public string TeamRoute { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri {
            get { return new Uri($"https://www.stoolball.org.uk/id/team/{TeamId}"); }
        }

        /// <summary>
        /// Gets a description of the team suitable for metadata or search results
        /// </summary>
        public string Description()
        {
            var description = new StringBuilder("A stoolball team");
            var location = MatchLocations.FirstOrDefault();
            if (location != null)
            {

                var placeName = new StringBuilder(location.Locality);
                if (!string.IsNullOrEmpty(location.Town))
                {
                    if (placeName.Length > 0)
                    {
                        placeName.Append(", ");
                    }
                    placeName.Append(location.Town);
                }
                if (!string.IsNullOrEmpty(location.AdministrativeArea))
                {
                    if (placeName.Length > 0)
                    {
                        placeName.Append(", ");
                    }
                    placeName.Append(location.AdministrativeArea);
                }
                if (placeName.Length > 0)
                {
                    description.Append(" in ").Append(placeName.ToString());
                }
            }

            if (Seasons.Count > 0)
            {
                var competitions = new Dictionary<Guid, string>();
                foreach (var teamInSeason in Seasons)
                {
                    if (teamInSeason?.Season?.Competition?.CompetitionId != null
                        && !competitions.ContainsKey(teamInSeason.Season.Competition.CompetitionId.Value)
                        && !string.IsNullOrEmpty(teamInSeason.Season.Competition.CompetitionName))
                    {
                        competitions.Add(teamInSeason.Season.Competition.CompetitionId.Value, teamInSeason.Season.Competition.CompetitionName);
                    }
                }

                if (competitions.Count > 0)
                {
                    description.Append(" playing in the ");
                    var keys = new List<Guid>(competitions.Keys);
                    for (var i = 0; i < keys.Count; i++)
                    {
                        description.Append(competitions[keys[i]]);
                        if (i < (competitions.Count - 2)) { description.Append(", "); }
                        if (i == (competitions.Count - 2)) { description.Append(" and "); }
                    }
                }
            }
            else
            {
                description.Append(" playing friendlies or tournaments");
            }
            description.Append('.');
            return description.ToString();
        }
    }
}
