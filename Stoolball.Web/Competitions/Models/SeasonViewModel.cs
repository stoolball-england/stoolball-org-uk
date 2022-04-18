using System;
using Stoolball.Competitions;
using Stoolball.Web.Matches.Models;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Competitions.Models
{
    public class SeasonViewModel : BaseViewModel
    {
        public SeasonViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public Season? Season { get; set; }
        public MatchListingViewModel Matches { get; set; } = new MatchListingViewModel();
        public AddMatchMenuViewModel AddMatchMenu { get; set; } = new AddMatchMenuViewModel();
        public Uri? UrlReferrer { get; set; }
        public string? GoogleMapsApiKey { get; set; }
    }
}