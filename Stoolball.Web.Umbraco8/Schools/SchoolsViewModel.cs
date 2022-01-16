using System.Collections.Generic;
using Stoolball.Listings;
using Stoolball.Schools;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Schools
{
    public class SchoolsViewModel : BaseViewModel, IListingsModel<School, SchoolFilter>
    {
        public SchoolsViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }

        public SchoolFilter Filter { get; set; } = new SchoolFilter();
        public List<School> Listings { get; internal set; } = new List<School>();
    }
}