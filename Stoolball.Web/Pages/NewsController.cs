using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Stoolball.Metadata;
using Stoolball.Web.Models;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

namespace Stoolball.Web.Pages
{
    public class NewsController : RenderController
    {
        private readonly IPublishedValueFallback _publishedValueFallback;
        private readonly IDocumentNavigationQueryService _navigationQueryService;
        private readonly IPublishedContentStatusFilteringService _publishedStatusFilteringService;

        public NewsController(ILogger<NewsController> logger, ICompositeViewEngine compositeViewEngine, IUmbracoContextAccessor umbracoContextAccessor, IPublishedValueFallback publishedValueFallback,
            IDocumentNavigationQueryService navigationQueryService, IPublishedContentStatusFilteringService publishedStatusFilteringService)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _publishedValueFallback = publishedValueFallback ?? throw new System.ArgumentNullException(nameof(publishedValueFallback));
            _navigationQueryService = navigationQueryService ?? throw new System.ArgumentNullException(nameof(navigationQueryService));
            _publishedStatusFilteringService = publishedStatusFilteringService ?? throw new System.ArgumentNullException(nameof(publishedStatusFilteringService));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true, YouTube = true)]
        public override IActionResult Index()
        {
            var model = new News(CurrentPage, _publishedValueFallback);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            if (int.TryParse(Request.Query["page"], out var pageNumber))
            {
                model.PageNumber = pageNumber;
            }

            var stories = CurrentPage!.Children(_navigationQueryService, _publishedStatusFilteringService).OfType<NewsStory>();
            model.TotalStories = stories.Count();
            model.Stories.AddRange(stories
                .OrderByDescending(x => x.DisplayDate == DateTime.MinValue ? x.CreateDate : x.DisplayDate)
                .Skip(model.PageSize * (pageNumber - 1))
                .Take(model.PageSize)
                );

            return CurrentTemplate(model);
        }
    }
}