using Stoolball.Matches;
using Stoolball.Web.Matches.Models;

namespace Stoolball.Web.Matches
{
    public class AddMatchMenuViewModelFactory : IAddMatchMenuViewModelFactory
    {
        public AddMatchMenuViewModel CreateModel(string? baseRoute, bool isFirstAdminButton, bool showTrainingSession, bool showFriendlyMatch, bool showKnockoutMatch, bool showLeagueMatch, bool showTournament)
        {
            var model = new AddMatchMenuViewModel
            {
                BaseRoute = baseRoute,
                IsFirstAdminButton = isFirstAdminButton,
                HasTournaments = showTournament
            };
            if (showFriendlyMatch) { model.MatchTypes.Add(MatchType.FriendlyMatch); }
            if (showKnockoutMatch) { model.MatchTypes.Add(MatchType.KnockoutMatch); }
            if (showLeagueMatch) { model.MatchTypes.Add(MatchType.LeagueMatch); }

            // training sessions are treated separately, because the wording is not "add match"
            model.HasTrainingSessions = showTrainingSession;
            model.HasSingleMatchType = model.MatchTypes.Count == 1;
            model.HasMultipleMatchTypes = model.MatchTypes.Count > 1;

            var totalButtonTypes = (model.HasTrainingSessions ? 1 : 0) + (model.HasSingleMatchType || model.HasMultipleMatchTypes ? 1 : 0) + (model.HasTournaments ? 1 : 0);

            model.TrainingIsInMenuAtSmall = (model.HasTrainingSessions && totalButtonTypes > 1);
            model.TrainingIsInMenuAtMedium = (totalButtonTypes == 3) || (model.HasTrainingSessions && model.HasTournaments) || (model.HasTrainingSessions && (model.HasSingleMatchType || model.HasMultipleMatchTypes) && !model.IsFirstAdminButton);
            model.TrainingIsInMenuAtLarge = (totalButtonTypes == 3 && !model.IsFirstAdminButton);
            model.TournamentIsInMenuAtSmall = (model.HasTournaments && totalButtonTypes > 1);
            model.TournamentIsInMenuAtMedium = (model.HasTournaments && totalButtonTypes == 3 && !model.IsFirstAdminButton) || (totalButtonTypes == 2 && model.HasTrainingSessions && model.HasTournaments) || (model.HasTournaments && (model.HasSingleMatchType || model.HasMultipleMatchTypes) && !model.IsFirstAdminButton);
            model.MatchIsInMenuAtSmall = (model.HasMultipleMatchTypes || (model.HasSingleMatchType && (model.TrainingIsInMenuAtSmall || model.TournamentIsInMenuAtSmall)));
            model.MatchIsInMenuAtMedium = (model.HasMultipleMatchTypes || (model.HasSingleMatchType && (model.TrainingIsInMenuAtMedium || model.TournamentIsInMenuAtMedium)));
            model.MatchIsInMenuAtLarge = model.HasMultipleMatchTypes || (model.HasSingleMatchType && (model.TrainingIsInMenuAtLarge || model.TournamentIsInMenuAtLarge));

            model.HasMenuAtSmall = model.TrainingIsInMenuAtSmall || model.MatchIsInMenuAtSmall || model.TournamentIsInMenuAtSmall;
            model.HasMenuAtMedium = model.TrainingIsInMenuAtMedium || model.MatchIsInMenuAtMedium || model.TournamentIsInMenuAtMedium;
            model.HasMenuAtLarge = model.TrainingIsInMenuAtLarge || model.MatchIsInMenuAtLarge || model.TournamentIsInMenuAtLarge;

            model.FirstButtonAtLarge = model.IsFirstAdminButton ? model.HasTrainingSessions ? AddMatchMenuButtonType.Training : (model.HasSingleMatchType || model.HasMultipleMatchTypes ? AddMatchMenuButtonType.MatchType : AddMatchMenuButtonType.Tournament) : AddMatchMenuButtonType.Other;

            model.FirstButtonAtMedium = (model.IsFirstAdminButton && totalButtonTypes == 3) ? AddMatchMenuButtonType.MatchType : model.FirstButtonAtLarge;
            if (model.FirstButtonAtMedium == AddMatchMenuButtonType.Training && model.TrainingIsInMenuAtMedium) { model.FirstButtonAtMedium = AddMatchMenuButtonType.MatchType; }

            model.LastButtonAtLarge = model.HasTournaments ? AddMatchMenuButtonType.Tournament : (model.HasSingleMatchType || model.HasMultipleMatchTypes ? AddMatchMenuButtonType.MatchType : AddMatchMenuButtonType.Training);
            if (model.HasTournaments && !model.TournamentIsInMenuAtMedium) { model.LastButtonAtMedium = AddMatchMenuButtonType.Tournament; }
            else if (model.HasMenuAtMedium || (model.HasSingleMatchType && !model.MatchIsInMenuAtMedium)) { model.LastButtonAtMedium = AddMatchMenuButtonType.MatchType; }
            else if (model.HasTrainingSessions) { model.LastButtonAtMedium = AddMatchMenuButtonType.Training; }

            return model;
        }
    }
}
