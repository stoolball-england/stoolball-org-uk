using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using HtmlAgilityPack;
using Humanizer;
using Stoolball.Logging;
using Stoolball.Teams;

namespace Stoolball.Competitions
{
    public class Competition : IAuditable
    {
        public Guid? CompetitionId { get; set; }

        [Display(Name = "Competition name")]
        [Required]
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

        [Display(Name = "Contact details for the public")]
        public string PublicContactDetails { get; set; }

        [Display(Name = "Contact details for Stoolball England")]
        public string PrivateContactDetails { get; set; }

        [Display(Name = "Competition website")]
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

        [Display(Name = "What year was this competition first played?")]
        public int? FromYear { get; set; }

        [Display(Name = "If it's no longer played, when was the last year?")]
        public int? UntilYear { get; set; }

        [Required]
        [Display(Name = "Who can play?")]
        public PlayerType PlayerType { get; set; }

        public Guid? MemberGroupKey { get; set; }

        public string MemberGroupName { get; set; }

        public string CompetitionRoute { get; set; }
        public List<AuditRecord> History { get; internal set; } = new List<AuditRecord>();

        public Uri EntityUri {
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
