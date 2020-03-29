using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Web.Models;
using Umbraco.Web.Mvc;

namespace Stoolball.Web.Routing
{
    /// <summary>
    /// Base controller which inherits from Umbraco's base controller and implements an async-compatible version of 
    /// <see cref="RenderMvcController.Index(ContentModel)"/>. Controllers routed through <see cref="StoolballRouterController"/> 
    /// should inherit from this and override <see cref="Index"/>. When <see cref="StoolballRouterController" /> invokes
    /// a specific controller type it can cast the return type as <c>RenderMvcControllerAsync</c> and itself support an 
    /// async <see cref="StoolballRouterController.Index(ContentModel)">Index</c> action.
    /// </summary>
    public abstract class RenderMvcControllerAsync : RenderMvcController
    {
        [HttpGet]
        public abstract new Task<ActionResult> Index(ContentModel contentModel);
    }
}