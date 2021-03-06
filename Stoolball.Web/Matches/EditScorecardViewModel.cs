﻿using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Web.Routing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;

namespace Stoolball.Web.Matches
{
    public class EditScorecardViewModel : BaseViewModel
    {
        public EditScorecardViewModel(IPublishedContent contentModel, IUserService userService) : base(contentModel, userService)
        {
        }

        public Match Match { get; set; }
        public int? InningsOrderInMatch { get; set; }
        public MatchInningsViewModel CurrentInnings { get; set; } = new MatchInningsViewModel();

        public IDateTimeFormatter DateFormatter { get; set; }

        public bool Autofocus { get; set; }
    }
}