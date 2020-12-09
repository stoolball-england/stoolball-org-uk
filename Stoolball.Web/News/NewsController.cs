using System;
using System.Linq;
using System.Web.Mvc;
using Stoolball.Metadata;
using Stoolball.Web.Security;
using Umbraco.Web;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;
using Umbraco.Web.PublishedModels;

namespace Stoolball.Web.ContentPage
{
    public class NewsController : RenderMvcController
    {
        [HttpGet]
        [ContentSecurityPolicy(Forms = true, TinyMCE = true, YouTube = true)]
        public override ActionResult Index(ContentModel contentModel)
        {
            var model = new News(contentModel?.Content);
            model.Metadata = new ViewMetadata
            {
                PageTitle = model.Name,
                Description = model.Description
            };

            if (int.TryParse(Request.QueryString["page"], out var pageNumber))
            {
                model.PageNumber = pageNumber;
            }

            var stories = model.Children<NewsStory>("newsStory");
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