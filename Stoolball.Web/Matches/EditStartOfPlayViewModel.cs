using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Web.Routing;
using System;
using System.ComponentModel.DataAnnotations;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Matches
{
    public class EditStartOfPlayViewModel : BaseViewModel
    {
        public EditStartOfPlayViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }
        public Match Match { get; set; }

        [Display(Name = "Ground or sports centre name")]
        public string MatchLocationName { get; set; }
        public Guid? MatchLocationId { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TeamInMatch.MatchTeamId"/> of the team that won the toss
        /// </summary>
        public Guid? TossWonBy { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TeamInMatch.MatchTeamId"/> of the team that batted first
        /// </summary>
        public Guid? BattedFirst { get; set; }

        public IDateTimeFormatter DateFormatter { get; set; }
    }
}