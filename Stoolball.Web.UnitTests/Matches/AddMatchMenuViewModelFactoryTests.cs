using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class AddMatchMenuViewModelFactoryTests
    {
        private AddMatchMenuViewModelFactory _addMatchMenuFactory;

        public AddMatchMenuViewModelFactoryTests()
        {
            _addMatchMenuFactory = new AddMatchMenuViewModelFactory();
        }

        #region Single button
        [Fact]
        public void Training_session_should_set_HasTrainingSessions()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, true, false, false, false, false);

            Assert.True(result.HasTrainingSessions);
            Assert.False(result.HasSingleMatchType);
            Assert.False(result.HasMultipleMatchTypes);
            Assert.False(result.HasTournaments);
        }

        [Fact]
        public void Training_session_only_should_be_first_and_last_and_outside_menu_from_small_upwards()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, true, false, false, false, false);

            Assert.Equal(AddMatchMenuButtonType.Training, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Training, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.Training, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Training, result.LastButtonAtLarge);
            Assert.False(result.HasMenuAtSmall);
            Assert.False(result.HasMenuAtMedium);
            Assert.False(result.HasMenuAtLarge);
            Assert.False(result.TrainingIsInMenuAtSmall);
            Assert.False(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.False(result.MatchIsInMenuAtSmall);
            Assert.False(result.MatchIsInMenuAtMedium);
            Assert.False(result.MatchIsInMenuAtLarge);
            Assert.False(result.TournamentIsInMenuAtSmall);
            Assert.False(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Friendly_match_type_only_should_set_HasSingleMatchType()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, false, true, false, false, false);

            Assert.False(result.HasTrainingSessions);
            Assert.True(result.HasSingleMatchType);
            Assert.False(result.HasMultipleMatchTypes);
            Assert.False(result.HasTournaments);
        }

        [Fact]
        public void Knockout_match_type_only_should_set_HasSingleMatchType()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, false, false, true, false, false);

            Assert.False(result.HasTrainingSessions);
            Assert.True(result.HasSingleMatchType);
            Assert.False(result.HasMultipleMatchTypes);
            Assert.False(result.HasTournaments);
        }

        [Fact]
        public void League_match_type_only_should_set_HasSingleMatchType()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, false, false, false, true, false);

            Assert.False(result.HasTrainingSessions);
            Assert.True(result.HasSingleMatchType);
            Assert.False(result.HasMultipleMatchTypes);
            Assert.False(result.HasTournaments);
        }

        [Fact]
        public void Friendly_match_type_only_should_be_first_and_last_and_outside_menu_from_small_upwards()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, false, true, false, false, false);

            AssertSingleMatchTypeShouldBeFirstAndLastAndOutsideMenuFromSmallUpwards(result);
        }

        [Fact]
        public void Knockout_match_type_only_should_be_first_and_last_and_outside_menu_from_small_upwards()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, false, false, true, false, false);

            AssertSingleMatchTypeShouldBeFirstAndLastAndOutsideMenuFromSmallUpwards(result);
        }

        [Fact]
        public void League_match_type_only_should_be_first_and_last_and_outside_menu_from_small_upwards()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, false, false, false, true, false);

            AssertSingleMatchTypeShouldBeFirstAndLastAndOutsideMenuFromSmallUpwards(result);
        }

        private static void AssertSingleMatchTypeShouldBeFirstAndLastAndOutsideMenuFromSmallUpwards(AddMatchMenuViewModel result)
        {
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtLarge);
            Assert.False(result.HasMenuAtSmall);
            Assert.False(result.HasMenuAtMedium);
            Assert.False(result.HasMenuAtLarge);
            Assert.False(result.TrainingIsInMenuAtSmall);
            Assert.False(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.False(result.MatchIsInMenuAtSmall);
            Assert.False(result.MatchIsInMenuAtMedium);
            Assert.False(result.MatchIsInMenuAtLarge);
            Assert.False(result.TournamentIsInMenuAtSmall);
            Assert.False(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Multiple_match_types_should_set_HasMultipleMatchTypes()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, false, true, false, true, false);

            Assert.False(result.HasTrainingSessions);
            Assert.False(result.HasSingleMatchType);
            Assert.True(result.HasMultipleMatchTypes);
            Assert.False(result.HasTournaments);
        }

        [Fact]
        public void Multiple_match_types_only_should_be_first_and_last_and_in_menu_from_small_upwards()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, false, true, false, true, false);

            Assert.Equal(AddMatchMenuButtonType.MatchType, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtLarge);
            Assert.True(result.HasMenuAtSmall);
            Assert.True(result.HasMenuAtMedium);
            Assert.True(result.HasMenuAtLarge);
            Assert.False(result.TrainingIsInMenuAtSmall);
            Assert.False(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.True(result.MatchIsInMenuAtSmall);
            Assert.True(result.MatchIsInMenuAtMedium);
            Assert.True(result.MatchIsInMenuAtLarge);
            Assert.False(result.TournamentIsInMenuAtSmall);
            Assert.False(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Tournament_should_set_HasTournaments()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, false, false, false, false, true);

            Assert.False(result.HasTrainingSessions);
            Assert.False(result.HasSingleMatchType);
            Assert.False(result.HasMultipleMatchTypes);
            Assert.True(result.HasTournaments);
        }

        [Fact]
        public void Tournament_only_should_be_first_and_last_and_visible_from_small_upwards()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, false, false, false, false, true);

            Assert.Equal(AddMatchMenuButtonType.Tournament, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtLarge);
            Assert.False(result.HasMenuAtSmall);
            Assert.False(result.HasMenuAtMedium);
            Assert.False(result.HasMenuAtLarge);
            Assert.False(result.TrainingIsInMenuAtSmall);
            Assert.False(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.False(result.MatchIsInMenuAtSmall);
            Assert.False(result.MatchIsInMenuAtMedium);
            Assert.False(result.MatchIsInMenuAtLarge);
            Assert.False(result.TournamentIsInMenuAtSmall);
            Assert.False(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Training_session_only_should_be_last_not_first_when_there_is_another_admin_button()
        {
            var result = _addMatchMenuFactory.CreateModel(null, false, true, false, false, false, false);

            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.Training, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Training, result.LastButtonAtLarge);
        }

        [Fact]
        public void Friendly_match_type_only_should_be_last_not_first_when_there_is_another_admin_button()
        {
            var result = _addMatchMenuFactory.CreateModel(null, false, false, true, false, false, false);

            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtLarge);
        }

        [Fact]
        public void Knockout_match_type_only_should_be_last_not_first_when_there_is_another_admin_button()
        {
            var result = _addMatchMenuFactory.CreateModel(null, false, false, false, true, false, false);

            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtLarge);
        }

        [Fact]
        public void League_match_type_only_should_be_last_not_first_when_there_is_another_admin_button()
        {
            var result = _addMatchMenuFactory.CreateModel(null, false, false, false, false, true, false);

            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtLarge);
        }

        [Fact]
        public void Multiple_match_types_only_should_be_last_not_first_when_there_is_another_admin_button()
        {
            var result = _addMatchMenuFactory.CreateModel(null, false, false, true, false, true, false);

            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtLarge);
        }

        [Fact]
        public void Tournament_only_should_be_last_not_first_when_there_is_another_admin_button()
        {
            var result = _addMatchMenuFactory.CreateModel(null, false, false, false, false, false, true);

            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtLarge);
        }
        #endregion

        #region Two buttons
        [Fact]
        public void Training_session_and_single_match_type_should_expand_at_medium_with_first_and_last_set()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, true, true, false, false, false);

            Assert.Equal(AddMatchMenuButtonType.Training, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Training, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtLarge);
            Assert.True(result.HasMenuAtSmall);
            Assert.False(result.HasMenuAtMedium);
            Assert.False(result.HasMenuAtLarge);
            Assert.True(result.TrainingIsInMenuAtSmall);
            Assert.False(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.True(result.MatchIsInMenuAtSmall);
            Assert.False(result.MatchIsInMenuAtMedium);
            Assert.False(result.MatchIsInMenuAtLarge);
            Assert.False(result.TournamentIsInMenuAtSmall);
            Assert.False(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Training_session_and_multiple_match_types_should_expand_at_medium_with_first_and_last_set()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, true, true, true, false, false);

            Assert.Equal(AddMatchMenuButtonType.Training, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Training, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtLarge);
            Assert.True(result.HasMenuAtSmall);
            Assert.True(result.HasMenuAtMedium);
            Assert.True(result.HasMenuAtLarge);
            Assert.True(result.TrainingIsInMenuAtSmall);
            Assert.False(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.True(result.MatchIsInMenuAtSmall);
            Assert.True(result.MatchIsInMenuAtMedium);
            Assert.True(result.MatchIsInMenuAtLarge);
            Assert.False(result.TournamentIsInMenuAtSmall);
            Assert.False(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Training_session_and_tournament_should_expand_at_large_with_first_and_last_set()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, true, false, false, false, true);

            Assert.Equal(AddMatchMenuButtonType.MatchType, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Training, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtLarge);
            Assert.True(result.HasMenuAtSmall);
            Assert.True(result.HasMenuAtMedium);
            Assert.False(result.HasMenuAtLarge);
            Assert.True(result.TrainingIsInMenuAtSmall);
            Assert.True(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.False(result.MatchIsInMenuAtSmall);
            Assert.False(result.MatchIsInMenuAtMedium);
            Assert.False(result.MatchIsInMenuAtLarge);
            Assert.True(result.TournamentIsInMenuAtSmall);
            Assert.True(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Single_match_type_and_tournament_should_expand_at_medium_with_first_and_last_set()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, false, false, false, true, true);

            Assert.Equal(AddMatchMenuButtonType.MatchType, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtLarge);
            Assert.True(result.HasMenuAtSmall);
            Assert.False(result.HasMenuAtMedium);
            Assert.False(result.HasMenuAtLarge);
            Assert.False(result.TrainingIsInMenuAtSmall);
            Assert.False(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.True(result.MatchIsInMenuAtSmall);
            Assert.False(result.MatchIsInMenuAtMedium);
            Assert.False(result.MatchIsInMenuAtLarge);
            Assert.True(result.TournamentIsInMenuAtSmall);
            Assert.False(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Multiple_match_types_and_tournament_should_expand_at_medium_with_first_and_last_set()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, false, false, true, true, true);

            Assert.Equal(AddMatchMenuButtonType.MatchType, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtLarge);
            Assert.True(result.HasMenuAtSmall);
            Assert.True(result.HasMenuAtMedium);
            Assert.True(result.HasMenuAtLarge);
            Assert.False(result.TrainingIsInMenuAtSmall);
            Assert.False(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.True(result.MatchIsInMenuAtSmall);
            Assert.True(result.MatchIsInMenuAtMedium);
            Assert.True(result.MatchIsInMenuAtLarge);
            Assert.True(result.TournamentIsInMenuAtSmall);
            Assert.False(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Training_session_and_single_match_type_should_expand_at_large_with_first_and_last_set_when_there_is_another_admin_button()
        {
            var result = _addMatchMenuFactory.CreateModel(null, false, true, true, false, false, false);

            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtLarge);
            Assert.True(result.HasMenuAtSmall);
            Assert.True(result.HasMenuAtMedium);
            Assert.False(result.HasMenuAtLarge);
            Assert.True(result.TrainingIsInMenuAtSmall);
            Assert.True(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.True(result.MatchIsInMenuAtSmall);
            Assert.True(result.MatchIsInMenuAtMedium);
            Assert.False(result.MatchIsInMenuAtLarge);
            Assert.False(result.TournamentIsInMenuAtSmall);
            Assert.False(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Training_session_and_multiple_match_types_should_expand_at_large_with_first_and_last_set_when_there_is_another_admin_button()
        {
            var result = _addMatchMenuFactory.CreateModel(null, false, true, true, true, false, false);

            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtLarge);
            Assert.True(result.HasMenuAtSmall);
            Assert.True(result.HasMenuAtMedium);
            Assert.True(result.HasMenuAtLarge);
            Assert.True(result.TrainingIsInMenuAtSmall);
            Assert.True(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.True(result.MatchIsInMenuAtSmall);
            Assert.True(result.MatchIsInMenuAtMedium);
            Assert.True(result.MatchIsInMenuAtLarge);
            Assert.False(result.TournamentIsInMenuAtSmall);
            Assert.False(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Training_session_should_not_be_first_or_last_when_there_is_another_admin_button_and_another_match_button()
        {
            var result = _addMatchMenuFactory.CreateModel(null, false, true, false, false, false, true);

            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtLarge);
            Assert.NotEqual(AddMatchMenuButtonType.Training, result.LastButtonAtMedium);
            Assert.NotEqual(AddMatchMenuButtonType.Training, result.LastButtonAtLarge);
        }

        [Fact]
        public void Single_match_type_and_tournament_should_expand_at_large_with_first_and_last_set_when_there_is_another_admin_button()
        {
            var result = _addMatchMenuFactory.CreateModel(null, false, false, true, false, false, true);

            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtLarge);
            Assert.True(result.HasMenuAtSmall);
            Assert.True(result.HasMenuAtMedium);
            Assert.False(result.HasMenuAtLarge);
            Assert.False(result.TrainingIsInMenuAtSmall);
            Assert.False(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.True(result.MatchIsInMenuAtSmall);
            Assert.True(result.MatchIsInMenuAtMedium);
            Assert.False(result.MatchIsInMenuAtLarge);
            Assert.True(result.TournamentIsInMenuAtSmall);
            Assert.True(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Multiple_match_types_and_tournament_should_expand_at_large_with_first_and_last_set_when_there_is_another_admin_button()
        {
            var result = _addMatchMenuFactory.CreateModel(null, false, false, true, true, false, true);

            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtLarge);
            Assert.True(result.HasMenuAtSmall);
            Assert.True(result.HasMenuAtMedium);
            Assert.True(result.HasMenuAtLarge);
            Assert.False(result.TrainingIsInMenuAtSmall);
            Assert.False(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.True(result.MatchIsInMenuAtSmall);
            Assert.True(result.MatchIsInMenuAtMedium);
            Assert.True(result.MatchIsInMenuAtLarge);
            Assert.True(result.TournamentIsInMenuAtSmall);
            Assert.True(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }
        #endregion

        #region Three buttons
        [Fact]
        public void Training_session_single_match_type_and_tournament_should_expand_tournament_at_medium_training_at_large_with_first_and_last_set()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, true, false, true, false, true);

            Assert.Equal(AddMatchMenuButtonType.MatchType, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Training, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtLarge);
            Assert.True(result.HasMenuAtSmall);
            Assert.True(result.HasMenuAtMedium);
            Assert.False(result.HasMenuAtLarge);
            Assert.True(result.TrainingIsInMenuAtSmall);
            Assert.True(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.True(result.MatchIsInMenuAtSmall);
            Assert.True(result.MatchIsInMenuAtMedium);
            Assert.False(result.MatchIsInMenuAtLarge);
            Assert.True(result.TournamentIsInMenuAtSmall);
            Assert.False(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Training_session_single_match_type_and_tournament_should_expand_tournament_at_large_with_first_and_last_set_when_there_is_another_admin_button()
        {
            var result = _addMatchMenuFactory.CreateModel(null, false, true, false, true, false, true);

            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtLarge);
            Assert.True(result.HasMenuAtSmall);
            Assert.True(result.HasMenuAtMedium);
            Assert.True(result.HasMenuAtLarge);
            Assert.True(result.TrainingIsInMenuAtSmall);
            Assert.True(result.TrainingIsInMenuAtMedium);
            Assert.True(result.TrainingIsInMenuAtLarge);
            Assert.True(result.MatchIsInMenuAtSmall);
            Assert.True(result.MatchIsInMenuAtMedium);
            Assert.True(result.MatchIsInMenuAtLarge);
            Assert.True(result.TournamentIsInMenuAtSmall);
            Assert.True(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Training_session_multiple_match_types_and_tournament_should_expand_tournament_at_medium_training_at_large_with_first_and_last_set()
        {
            var result = _addMatchMenuFactory.CreateModel(null, true, true, false, true, true, true);

            Assert.Equal(AddMatchMenuButtonType.MatchType, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Training, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtLarge);
            Assert.True(result.HasMenuAtSmall);
            Assert.True(result.HasMenuAtMedium);
            Assert.True(result.HasMenuAtLarge);
            Assert.True(result.TrainingIsInMenuAtSmall);
            Assert.True(result.TrainingIsInMenuAtMedium);
            Assert.False(result.TrainingIsInMenuAtLarge);
            Assert.True(result.MatchIsInMenuAtSmall);
            Assert.True(result.MatchIsInMenuAtMedium);
            Assert.True(result.MatchIsInMenuAtLarge);
            Assert.True(result.TournamentIsInMenuAtSmall);
            Assert.False(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }

        [Fact]
        public void Training_session_multiple_match_types_and_tournament_should_expand_tournament_at_large_with_first_and_last_set_when_there_is_another_admin_button()
        {
            var result = _addMatchMenuFactory.CreateModel(null, false, true, false, true, true, true);

            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Other, result.FirstButtonAtLarge);
            Assert.Equal(AddMatchMenuButtonType.MatchType, result.LastButtonAtMedium);
            Assert.Equal(AddMatchMenuButtonType.Tournament, result.LastButtonAtLarge);
            Assert.True(result.HasMenuAtSmall);
            Assert.True(result.HasMenuAtMedium);
            Assert.True(result.HasMenuAtLarge);
            Assert.True(result.TrainingIsInMenuAtSmall);
            Assert.True(result.TrainingIsInMenuAtMedium);
            Assert.True(result.TrainingIsInMenuAtLarge);
            Assert.True(result.MatchIsInMenuAtSmall);
            Assert.True(result.MatchIsInMenuAtMedium);
            Assert.True(result.MatchIsInMenuAtLarge);
            Assert.True(result.TournamentIsInMenuAtSmall);
            Assert.True(result.TournamentIsInMenuAtMedium);
            Assert.False(result.TournamentIsInMenuAtLarge);
        }
        #endregion
    }
}
