using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Stoolball.Web.Routing
{
    public interface IStoolballRouterController
    {
        Task<IActionResult> Index();
        ControllerContext ControllerContext { get; set; }
        ModelStateDictionary ModelState { get; }
    }
}