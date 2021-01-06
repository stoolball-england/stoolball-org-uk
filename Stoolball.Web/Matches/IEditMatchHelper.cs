using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web.Mvc;
using Stoolball.Competitions;

namespace Stoolball.Web.Matches
{
    public interface IEditMatchHelper
    {
        List<SelectListItem> PossibleTeamsAsListItems(IEnumerable<TeamInSeason> teams);
        List<SelectListItem> PossibleSeasonsAsListItems(IEnumerable<Season> seasons);
        Task ConfigureModelPossibleTeams(IEditMatchViewModel model, IEnumerable<Season> possibleSeasons);
        void ConfigureAddMatchModelMetadata(IEditMatchViewModel model);
        void ConfigureModelFromRequestData(IEditMatchViewModel model, NameValueCollection unvalidatedFormData, NameValueCollection formData, ModelStateDictionary modelState);
    }
}