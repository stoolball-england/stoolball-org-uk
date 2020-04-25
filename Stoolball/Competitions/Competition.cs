using HtmlAgilityPack;
using Humanizer;
using Stoolball.Audit;
using Stoolball.Teams;
using System;
using System.Collections.Generic;

namespace Stoolball.Competitions
{
    public class Competition : IAuditable
    {
        public int? CompetitionId { get; set; }

        public string CompetitionName { get; set; }

        /// <summary>
        /// Gets the name of the competition and the type of players (if not stated in the name)
        /// </summary>
        /// <returns></returns>
        public string CompetitionNameAndPlayerType()
        {
            var name = CompetitionName;

            var type = PlayerType.ToString().Humanize(LetterCasing.Sentence);
            if (!name.Replace("'", string.Empty).ToUpperInvariant().Contains(type.Replace("'", string.Empty).ToUpperInvariant()))
            {
                name += " (" + type + ")";
            }
            return name;
        }

        public List<Season> Seasons { get; internal set; } = new List<Season>();

        public string Introduction { get; set; }

        public string PublicContactDetails { get; set; }

        public string PrivateContactDetails { get; set; }

        public string Website { get; set; }

        public string Twitter { get; set; }

        public string Facebook { get; set; }

        public string Instagram { get; set; }

        public string YouTube { get; set; }

        public DateTimeOffset FromDate { get; set; }

        public DateTimeOffset? UntilDate { get; set; }

        public PlayerType PlayerType { get; set; }

        public int PlayersPerTeam { get; set; } = 11;

        public int Overs { get; set; } = 16;

        public string CompetitionRoute { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri
        {
            get { return new Uri($"https://www.stoolball.org.uk/id/competition/{CompetitionId}"); }
        }

        /// <summary>
        /// Gets a description of the competition suitable for metadata or search results
        /// </summary>
        /// <returns></returns>
        public string Description()
        {
            if (!string.IsNullOrEmpty(Introduction))
            {
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(Introduction.Trim());
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
