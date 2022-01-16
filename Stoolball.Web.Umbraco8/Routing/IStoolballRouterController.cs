using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Models;

namespace Stoolball.Web.Routing
{
    public interface IStoolballRouterController
    {
        Task<ActionResult> Index(ContentModel contentModel);
        ControllerContext ControllerContext { get; set; }
        ModelStateDictionary ModelState { get; }
    }
}