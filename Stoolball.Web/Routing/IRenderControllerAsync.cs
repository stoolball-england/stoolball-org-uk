using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Stoolball.Web.Routing
{
    /// <summary>
    /// Interface for controllers which inherit from Umbraco's base controller and implement an async-compatible version of 
    /// <see cref="RenderController.Index()"/>. Controllers routed through <see cref="StoolballRouterController"/> 
    /// should implement from this and override <see cref="Index"/> using the 'new' keyword. When <see cref="StoolballRouterController" /> 
    /// invokes a specific controller type it can cast the return type as <c>IRenderControllerAsync</c> and itself support an 
    /// async <see cref="StoolballRouterController.Index()">Index</c> action.
    /// </summary>
    public interface IRenderControllerAsync
    {
        Task<IActionResult> Index();
        ControllerContext ControllerContext { get; set; }
        ModelStateDictionary ModelState { get; }
    }
}