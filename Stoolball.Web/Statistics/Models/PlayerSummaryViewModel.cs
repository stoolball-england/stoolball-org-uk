using Stoolball.Statistics;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Statistics.Models
{
    public class PlayerSummaryViewModel : BasePlayerViewModel
    {
        public PlayerSummaryViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }

        public BattingStatistics BattingStatistics { get; set; } = new();
        public BowlingStatistics BowlingStatistics { get; set; } = new();
        public FieldingStatistics FieldingStatistics { get; set; } = new();
        public bool IsCurrentMember { get; set; }
        public bool ShowPlayerLinkedToMemberConfirmation { get; set; }
        public int? TotalPlayerOfTheMatchAwards { get; set; }
    }
}