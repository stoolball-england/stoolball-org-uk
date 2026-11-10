using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Web.Models;
using Stoolball.Web.Pages;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models.PublishedContent;
using Xunit;

namespace Stoolball.Web.UnitTests.Pages
{
    public class NewsControllerTests : UmbracoBaseTest
    {
        private NewsController CreateController()
        {
            SetupPropertyValue(CurrentPage, "description", string.Empty);
            return new NewsController(
                Mock.Of<ILogger<NewsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                Mock.Of<IVariationContextAccessor>(),
                ServiceContext)
            {
                ControllerContext = ControllerContext
            };
        }

        private IPublishedContent CreateStory(string name, DateTime displayDate, DateTime? createDate = null)
        {
            var story = new Mock<IPublishedContent>();
            story.Setup(x => x.Name).Returns(name);
            story.Setup(x => x.CreateDate).Returns(createDate ?? DateTime.MinValue);

            var property = new Mock<IPublishedProperty>();
            property.Setup(x => x.Alias).Returns("displayDate");
            property.Setup(x => x.GetValue(null, null)).Returns(displayDate);
            property.Setup(x => x.HasValue(null, null)).Returns(true);
            story.Setup(x => x.GetProperty("displayDate")).Returns(property.Object);
            return new NewsStory(story.Object, new PublishedValueFallback(ServiceContext, Mock.Of<IVariationContextAccessor>()));
        }

        private void SetupStories(params IPublishedContent[] stories)
        {
            CurrentPage.Setup(x => x.ChildrenForAllCultures).Returns(stories.ToList());
            CurrentPage.Setup(x => x.Children).Returns(stories.ToList());
        }

        private void SetupPageQuery(string page)
        {
            Request.SetupGet(x => x.Query).Returns(new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> { { "page", page } }));
        }

        [Fact]
        public void Sets_name_and_description_from_content()
        {
            using var controller = CreateController();
            CurrentPage.Setup(x => x.Name).Returns("News");
            SetupStories();
            SetupPropertyValue(CurrentPage, "description", "This is the description");

            var result = controller.Index();

            var meta = ((IHasViewMetadata)((ViewResult)result).Model!).Metadata;
            Assert.Equal("News", meta.PageTitle);
            Assert.Equal("This is the description", meta.Description);
        }

        [Fact]
        public void Returns_News_ModelsBuilder_model()
        {
            using var controller = CreateController();
            SetupStories();

            var result = controller.Index();

            Assert.IsType<News>(((ViewResult)result).Model);
        }

        [Fact]
        public void Defaults_to_first_page_when_page_query_is_missing()
        {
            using var controller = CreateController();
            SetupStories();

            var model = (News)((ViewResult)controller.Index()).Model!;

            Assert.Equal(1, model.PageNumber);
        }

        [Fact]
        public void Reads_page_number_from_query()
        {
            using var controller = CreateController();
            SetupPageQuery("2");
            SetupStories();

            var model = (News)((ViewResult)controller.Index()).Model!;

            Assert.Equal(2, model.PageNumber);
        }

        [Fact]
        public void Ignores_non_numeric_page_query()
        {
            using var controller = CreateController();
            SetupPageQuery("abc");
            SetupStories();

            var model = (News)((ViewResult)controller.Index()).Model!;

            Assert.Equal(1, model.PageNumber);
        }

        [Fact]
        public void Counts_all_stories_and_returns_newest_first()
        {
            using var controller = CreateController();
            SetupStories(
                CreateStory("Oldest", new DateTime(2020, 1, 1)),
                CreateStory("Newest", new DateTime(2022, 1, 1)),
                CreateStory("Middle", new DateTime(2021, 1, 1)));

            var model = (News)((ViewResult)controller.Index()).Model!;

            Assert.Equal(3, model.TotalStories);
            Assert.Equal(new[] { "Newest", "Middle", "Oldest" }, model.Stories.Select(x => x.Name));
        }

        [Fact]
        public void Uses_create_date_when_display_date_is_not_set()
        {
            using var controller = CreateController();
            SetupStories(
                CreateStory("Displayed 2021", new DateTime(2021, 1, 1)),
                CreateStory("Created 2022", DateTime.MinValue, new DateTime(2022, 1, 1)),
                CreateStory("Displayed 2020", new DateTime(2020, 1, 1)));

            var model = (News)((ViewResult)controller.Index()).Model!;

            Assert.Equal(new[] { "Created 2022", "Displayed 2021", "Displayed 2020" }, model.Stories.Select(x => x.Name));
        }

        [Fact]
        public void Returns_only_one_page_of_stories()
        {
            using var controller = CreateController();
            SetupPageQuery("2");
            SetupStories(Enumerable.Range(1, 12)
                .Select(i => CreateStory($"Story {i}", new DateTime(2020, 1, 1).AddDays(i)))
                .ToArray());

            var model = (News)((ViewResult)controller.Index()).Model!;

            Assert.Equal(12, model.TotalStories);
            Assert.Equal(2, model.Stories.Count);
            Assert.Equal(new[] { "Story 2", "Story 1" }, model.Stories.Select(x => x.Name));
        }

        [Fact]
        public void Has_content_security_policy()
        {
            var method = typeof(NewsController).GetMethod(nameof(NewsController.Index))!;
            var attribute = method.GetCustomAttributes(typeof(ContentSecurityPolicyAttribute), false).SingleOrDefault() as ContentSecurityPolicyAttribute;

            Assert.NotNull(attribute);
            Assert.True(attribute!.Forms);
            Assert.True(attribute.TinyMCE);
            Assert.True(attribute.YouTube);
            Assert.False(attribute.GettyImages);
            Assert.False(attribute.GoogleMaps);
            Assert.False(attribute.GoogleGeocode);
        }
    }
}
