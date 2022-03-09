using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stoolball.Competitions;
using Stoolball.Web.Matches.Models;

namespace Stoolball.Web.Matches
{
    public interface IEditMatchHelper
    {
        List<SelectListItem> PossibleTeamsAsListItems(IEnumerable<TeamInSeason> teams);
        List<SelectListItem> PossibleSeasonsAsListItems(IEnumerable<Season> seasons);
        Task ConfigureModelPossibleTeams(IEditMatchViewModel model, IEnumerable<Season> possibleSeasons);
        void ConfigureAddMatchModelMetadata(IEditMatchViewModel model);
        void ConfigureModelFromRequestData(IEditMatchViewModel model, IFormCollection formData, ModelStateDictionary modelState);
    }
}