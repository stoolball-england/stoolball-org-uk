using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Statistics;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Matches.Models
{
    public class DeleteMatchViewModel : BaseViewModel
    {
        public DeleteMatchViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public Match? Match { get; set; }
        public MatchingTextConfirmation ConfirmDeleteRequest { get; set; } = new MatchingTextConfirmation();
        public bool Deleted { get; set; }
        public int TotalComments { get; set; }
        public List<PlayerIdentity> PlayerIdentities { get; internal set; } = new List<PlayerIdentity>();
    }
}