using System.Collections.Generic;
using Stoolball.Listings;
using Stoolball.Schools;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Schools
{
    public class SchoolsViewModel : BaseViewModel, IListingsModel<School, SchoolFilter>
    {
        public SchoolsViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }

        public SchoolFilter Filter { get; set; } = new SchoolFilter();
        public List<School> Listings { get; internal set; } = new List<School>();
    }
}