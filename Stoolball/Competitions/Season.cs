using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using HtmlAgilityPack;
using Humanizer;
using Stoolball.Logging;
using Stoolball.Matches;

namespace Stoolball.Competitions
{
    public class Season : IAuditable
    {
        [Display(Name = "Season")]
        public Guid? SeasonId { get; set; }

        /// <summary>
        /// Gets the name of the season, not including the competition name
        /// </summary>
        /// <returns></returns>
        public string SeasonName()
        {
            if (FromYear == UntilYear)
            {
                return $"{FromYear} season";
            }
            else
            {
                return $"{FromYear}/{new DateTime(UntilYear, 1, 1).ToString("yy", CultureInfo.CurrentCulture)} season";
            }
        }

        /// <summary>
        /// Gets the name of the competition and season
        /// </summary>
        /// <returns></returns>
        public string SeasonFullName()
        {
            return $"{Competition?.CompetitionName}, {SeasonName()}";
        }

        /// <summary>
        /// Gets the name of the competition and season, and the type of players (if not stated in the name)
        /// </summary>
        /// <returns></returns>
        public string SeasonFullNameAndPlayerType()
        {
            var competitionName = Competition?.CompetitionName ?? string.Empty;
            var playerType = string.Empty;

            var type = Competition?.PlayerType.ToString().Humanize(LetterCasing.Sentence);
            if (type != null && !competitionName.Replace("'", string.Empty).ToUpperInvariant().Contains(type.Replace("'", string.Empty).ToUpperInvariant()))
            {
                playerType = " (" + type + ")";
            }

            return $"{SeasonFullName()}{playerType}".Trim();
        }

        public Competition Competition { get; set; }

        [Display(Name = "What year does the season start?")]
        [Required]
        public int FromYear { get; set; }

        [Required]
        public int UntilYear { get; set; }

        public string Introduction { get; set; }

        [Display(Name = "How many players on a team?")]
        public int? PlayersPerTeam { get; set; }

        [Display(Name = "How many overs in a match?")]
        public int? Overs { get; set; }

        public bool EnableLastPlayerBatsOn { get; set; }
        public bool EnableBonusOrPenaltyRuns { get; set; }
        public bool EnableTournaments { get; set; }
        public List<MatchType> MatchTypes { get; internal set; } = new List<MatchType>();

        public List<TeamInSeason> Teams { get; internal set; } = new List<TeamInSeason>();
        public List<PointsRule> PointsRules { get; internal set; } = new List<PointsRule>();
        public List<PointsAdjustment> PointsAdjustments { get; internal set; } = new List<PointsAdjustment>();

        [Display(Name = "Results commentary")]
        public string Results { get; set; }

        [Display(Name = "results table type")]
        public ResultsTableType ResultsTableType { get; set; }

        public bool EnableRunsScored { get; set; }

        public bool EnableRunsConceded { get; set; }

        public string SeasonRoute { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri {
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
                description.Append('.');
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
