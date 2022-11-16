using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Stoolball.Statistics;
using Stoolball.Web.Filtering;
using Stoolball.Web.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;

namespace Stoolball.Web.Statistics.Models
{
    public abstract class StatisticsViewModel : BaseViewModel
    {
        public StatisticsViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public StatisticsFilter DefaultFilter { get; set; } = new();
        public StatisticsFilter AppliedFilter { get; set; } = new();
        public FilterViewModel FilterViewModel { get; set; } = new();
        public IHtmlContent? MinimumQualifyingInningsTemplate { get; set; }
        public bool ShowLinkOnly { get; set; }
        public bool ShowCaption { get; set; } = true;
        public bool ShowPlayerColumn { get; set; } = true;
        public bool LinkPlayer { get; set; } = true;
        public bool ShowTeamsColumn { get; set; } = true;
        public string? Heading { get; set; }
        public string? PartialView { get; set; }
        public abstract bool HasResults();
    }

    public class StatisticsViewModel<T> : StatisticsViewModel
    {
        public StatisticsViewModel(IPublishedContent? contentModel = null, IUserService? userService = null) : base(contentModel, userService)
        {
        }
        public List<StatisticsResult<T>> Results { get; internal set; } = new();

        public override bool HasResults() => Results.Any();
    }
}