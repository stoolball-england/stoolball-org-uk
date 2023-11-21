using System.Collections.Generic;
using Stoolball.Matches;
using Stoolball.Web.Matches.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Competitions.Models
{
    public class SeasonResultsTableViewModel : MatchListingViewModel
    {
        public SeasonResultsTableViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService) { }

        public IEnumerable<ResultsTableRow> ResultsTableRows = new List<ResultsTableRow>();

        public IEnumerable<MatchListing> MatchesAwaitingResults = new List<MatchListing>();
    }
}
