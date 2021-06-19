using System;
using Stoolball.Competitions;
using Stoolball.Web.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Competitions
{
    public class SeasonViewModel : BaseViewModel
    {
        public SeasonViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }
        public Season Season { get; set; }
        public MatchListingViewModel Matches { get; set; }
        public Uri UrlReferrer { get; set; }
        public string GoogleMapsApiKey { get; set; }
    }
}