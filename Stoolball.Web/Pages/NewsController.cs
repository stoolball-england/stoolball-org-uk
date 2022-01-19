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
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Stoolball.Web.Pages
{
    public class NewsController : RenderController
    {
        private readonly IVariationContextAccessor _variationContextAccessor;
        private readonly ServiceContext _serviceContext;

        public NewsController(ILogger<NewsController> logger, ICompositeViewEngine compositeViewEngine, IUmbracoContextAccessor umbracoContextAccessor, IVariationContextAccessor variationContextAccessor, ServiceContext context)
            : base(logger, compositeViewEngine, umbracoContextAccessor)
        {
            _variationContextAccessor = variationContextAccessor ?? throw new System.ArgumentNullException(nameof(variationContextAccessor));
            _serviceContext = context ?? throw new System.ArgumentNullException(nameof(context));
        }

        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true, YouTube = true)]
        public override IActionResult Index()
        {
            var model = new News(CurrentPage, new PublishedValueFallback(_serviceContext, _variationContextAccessor));
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            if (int.TryParse(Request.Query["page"], out var pageNumber))
            {
                model.PageNumber = pageNumber;
            }

            var stories = model.Children.OfType<NewsStory>();
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