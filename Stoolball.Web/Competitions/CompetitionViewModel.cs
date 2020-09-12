using System;
using Stoolball.Competitions;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Competitions
{
    public class CompetitionViewModel : BaseViewModel
    {
        public CompetitionViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public Competition Competition { get; set; }

        public Uri UrlReferrer { get; set; }
    }
}