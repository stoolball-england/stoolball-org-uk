using HtmlAgilityPack;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stoolball.Competitions
{
    public class Season
    {
        public int? SeasonId { get; set; }

        /// <summary>
        /// Gets the name of the season, not including the competition name
        /// </summary>
        /// <returns></returns>
        public string SeasonName()
        {
            if (StartYear == EndYear)
            {
                return $"{StartYear} season";
            }
            else
            {
                return $"{StartYear}/{EndYear} season";
            }
        }

        /// <summary>
        /// Gets the name of the competition and season, and the type of players (if not stated in the name)
        /// </summary>
        /// <returns></returns>
        public string SeasonFullNameAndPlayerType()
        {
            var name = Competition?.CompetitionName;

            var type = Competition?.PlayerType.ToString().Humanize(LetterCasing.Sentence);
            if (type != null && !name.Replace("'", string.Empty).ToUpperInvariant().Contains(type.Replace("'", string.Empty).ToUpperInvariant()))
            {
                name += " (" + type + ")";
            }

            return $"{name}, {SeasonName()}".Trim();
        }

        public Competition Competition { get; set; }

        public bool IsLatestSeason { get; set; }

        public int StartYear { get; set; }

        public int EndYear { get; set; }

        public string Introduction { get; set; }

        public List<TeamInSeason> Teams { get; internal set; } = new List<TeamInSeason>();

        public string Results { get; set; }

        public bool ShowTable { get; set; }

        public bool ShowRunsScored { get; set; }

        public bool ShowRunsConceded { get; set; }

        public string SeasonRoute { get; set; }
        public DateTimeOffset? DateCreated { get; set; }
        public DateTimeOffset? DateUpdated { get; set; }
        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/season/{SeasonId}"); }
        }

        /// <summary>
        /// Gets a description of the season suitable for metadata or search results
        /// </summary>
        /// <returns></returns>
        public string Description()
        {
            if (Teams.Count > 0)
            {
                var description = new StringBuilder("The ").Append(SeasonName()).Append(" season of a stoolball competition played by ");
                var totalTeamsToList = Teams.Count;
                if (totalTeamsToList > 10)
                {
                    description.Append("teams including ");
                    totalTeamsToList = 10;
                }
                for (var i = 0; i < totalTeamsToList; i++)
                {
                    description.Append(Teams[i].Team.TeamName);
                    if (i < (totalTeamsToList - 2)) description.Append(", ");
                    if (i == (totalTeamsToList - 2)) description.Append(" and ");
                }
                description.Append(".");
                return description.ToString();
            }
            else if (!string.IsNullOrEmpty(Competition?.Introduction))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(Competition.Introduction.Trim());
                var description = htmlDoc.DocumentNode.InnerText;

                var newLine = description.IndexOf(Environment.NewLine, StringComparison.OrdinalIgnoreCase);
                if (newLine > -1)
                {
                    description = description.Substring(0, newLine).TrimEnd();
                }
                return description;
            }
            else return string.Empty;
        }
    }
}
