using Stoolball.Web.Matches.Models;

namespace Stoolball.Web.Matches
{
    public interface IAddMatchMenuViewModelFactory
    {
        AddMatchMenuViewModel CreateModel(string? baseRoute, bool isFirstAdminButton, bool showTrainingSession, bool showFriendlyMatch, bool showKnockoutMatch, bool showLeagueMatch, bool showTournament);
    }
}