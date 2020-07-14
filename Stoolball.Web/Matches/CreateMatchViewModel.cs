using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Web.Routing;
using System.Collections.Generic;
using System.Web.Mvc;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Matches
{
    public class CreateMatchViewModel : BaseViewModel
    {
        public CreateMatchViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }

        public Match Match { get; set; }
        public Team Team { get; set; }
        public Season Season { get; set; }
        public List<SelectListItem> PossibleSeasons { get; internal set; } = new List<SelectListItem>();
    }
}