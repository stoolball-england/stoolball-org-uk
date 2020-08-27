using System.Threading.Tasks;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Services;
using Umbraco.Web;
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
        public RenderMvcControllerAsync() : base() { }
        public RenderMvcControllerAsync(IGlobalSettings globalSettings,
            IUmbracoContextAccessor umbracoContextAccessor,
            ServiceContext serviceContext,
            AppCaches appCaches,
            IProfilingLogger profilingLogger,
            UmbracoHelper umbracoHelper)
            : base(globalSettings, umbracoContextAccessor, serviceContext, appCaches, profilingLogger, umbracoHelper) { }

        [HttpGet]
        public abstract new Task<ActionResult> Index(ContentModel contentModel);

        /// <summary>
        /// Gets an action result based on the template name found in the route values and a model
        /// </summary>
        /// <remarks>This method exists to make it easier to override the base method in unit tests.</remarks>
        protected new virtual ActionResult CurrentTemplate<T>(T model)
        {
            return base.CurrentTemplate<T>(model);
        }
    }
}