using Stoolball.Clubs;
using Stoolball.Web.Routing;
using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Models;

namespace Stoolball.Web.Clubs
{
    public class ClubController : RenderMvcControllerAsync
    {
        [HttpGet]
        public override Task<ActionResult> Index(ContentModel contentModel)
        {
            if (contentModel is null)
            {
                throw new System.ArgumentNullException(nameof(contentModel));
            }

            var model = new ClubViewModel(contentModel.Content)
            {
                Club = new Club { ClubName = Umbraco.AssignedContentItem.Name }
            };
            return Task.FromResult(CurrentTemplate(model));
        }
    }
}