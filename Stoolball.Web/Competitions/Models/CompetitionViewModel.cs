using System;
using Stoolball.Competitions;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Competitions.Models
{
    public class CompetitionViewModel : BaseViewModel
    {
        public CompetitionViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public Competition? Competition { get; set; }

        public Uri? UrlReferrer { get; set; }
    }
}