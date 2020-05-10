using Stoolball.Clubs;
using Stoolball.Umbraco.Data.Clubs;
using Stoolball.Web.Routing;
using System.Collections.Generic;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Clubs
{
    public class ClubsViewModel : BaseViewModel
    {
        public ClubsViewModel(IPublishedContent contentModel) : base(contentModel)
        {
        }

        public ClubQuery ClubQuery { get; set; } = new ClubQuery();
        public List<Club> Clubs { get; internal set; } = new List<Club>();
    }
}