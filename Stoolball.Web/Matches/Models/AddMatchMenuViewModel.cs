using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Stoolball.Matches;

namespace Stoolball.Web.Matches.Models
{
    [ExcludeFromCodeCoverage]
    public class AddMatchMenuViewModel
    {
        public List<MatchType> MatchTypes { get; set; } = new List<MatchType>();
        public string? BaseRoute { get; set; }
        public bool HasTournaments { get; set; }
        public bool IsFirstAdminButton { get; set; }

        public bool HasTrainingSessions { get; set; }

        public bool HasSingleMatchType { get; set; }
        public bool HasMultipleMatchTypes { get; set; }

        public bool HasMenuAtSmall { get; set; }
        public bool HasMenuAtMedium { get; set; }
        public bool HasMenuAtLarge { get; set; }

        public bool TrainingIsInMenuAtSmall { get; set; }
        public bool TrainingIsInMenuAtMedium { get; set; }
        public bool TrainingIsInMenuAtLarge { get; set; }
        public bool TournamentIsInMenuAtSmall { get; set; }
        public bool TournamentIsInMenuAtMedium { get; set; }
        public bool TournamentIsInMenuAtLarge { get; set; }
        public bool MatchIsInMenuAtSmall { get; set; }
        public bool MatchIsInMenuAtMedium { get; set; }
        public bool MatchIsInMenuAtLarge { get; set; }
        public AddMatchMenuButtonType FirstButtonAtLarge { get; set; }
        public AddMatchMenuButtonType FirstButtonAtMedium { get; set; }
        public AddMatchMenuButtonType LastButtonAtLarge { get; set; }
        public AddMatchMenuButtonType LastButtonAtMedium { get; set; }
    }
}