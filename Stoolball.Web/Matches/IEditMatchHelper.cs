using Stoolball.Competitions;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Stoolball.Web.Matches
{
    public interface IEditMatchHelper
    {
        List<SelectListItem> PossibleTeamsAsListItems(IEnumerable<TeamInSeason> teams);
        List<SelectListItem> PossibleSeasonsAsListItems(IEnumerable<Season> seasons);
        Task ConfigureModelPossibleTeams(IEditMatchViewModel model, IEnumerable<Season> possibleSeasons);
        void ConfigureModelHomeTeamAndLocation(IEditMatchViewModel model);
        void ConfigureAddMatchModelMetadata(IEditMatchViewModel model);
        void ConfigureModelFromRequestData(IEditMatchViewModel model, NameValueCollection unvalidatedFormData, NameValueCollection formData);
    }
}