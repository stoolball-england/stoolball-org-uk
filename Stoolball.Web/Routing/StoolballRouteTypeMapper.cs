using System;
using System.Collections.Generic;
using Stoolball.Web.Clubs;
using Stoolball.Web.Competitions;
using Stoolball.Web.Matches;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.Schools;
using Stoolball.Web.Teams;

namespace Stoolball.Web.Routing
{
    public class StoolballRouteTypeMapper : IStoolballRouteTypeMapper
    {
        private readonly Dictionary<StoolballRouteType, Type> _supportedControllers = new()
        {
            { StoolballRouteType.ClubActions, typeof(ClubActionsController) },
            { StoolballRouteType.Club, typeof(ClubController) },
            { StoolballRouteType.CreateClub, typeof(CreateClubController) },
            { StoolballRouteType.EditClub, typeof(EditClubController) },
            { StoolballRouteType.DeleteClub, typeof(DeleteClubController) },
            { StoolballRouteType.MatchesForClub, typeof(MatchesForClubController) },
            { StoolballRouteType.Teams, typeof(TeamsController) },
            { StoolballRouteType.TeamsMap, typeof(TeamsMapController) },
            { StoolballRouteType.Team, typeof(TeamController) },
            { StoolballRouteType.TeamActions, typeof(TeamActionsController) },
            { StoolballRouteType.CreateTeam, typeof(CreateTeamController) },
            { StoolballRouteType.EditTeam, typeof(EditTeamController) },
            { StoolballRouteType.DeleteTeam, typeof(DeleteTeamController) },
            { StoolballRouteType.MatchesForTeam, typeof(MatchesForTeamController) },
            { StoolballRouteType.TransientTeam, typeof(TransientTeamController) },
            { StoolballRouteType.EditTransientTeam, typeof(EditTransientTeamController) },
            { StoolballRouteType.MatchLocation, typeof(MatchLocationController) },
            { StoolballRouteType.MatchLocationActions, typeof(MatchLocationActionsController) },
            { StoolballRouteType.CreateMatchLocation, typeof(CreateMatchLocationController) },
            { StoolballRouteType.EditMatchLocation, typeof(EditMatchLocationController) },
            { StoolballRouteType.DeleteMatchLocation, typeof(DeleteMatchLocationController) },
            { StoolballRouteType.MatchLocations, typeof(MatchLocationsController) },
            { StoolballRouteType.MatchesForMatchLocation, typeof(MatchesForMatchLocationController) },
            { StoolballRouteType.Competitions, typeof(CompetitionsController) },
            { StoolballRouteType.Competition, typeof(CompetitionController) },
            { StoolballRouteType.CompetitionActions, typeof(CompetitionActionsController) },
            { StoolballRouteType.CreateCompetition, typeof(CreateCompetitionController) },
            { StoolballRouteType.EditCompetition, typeof(EditCompetitionController) },
            { StoolballRouteType.DeleteCompetition, typeof(DeleteCompetitionController) },
            { StoolballRouteType.Season, typeof(SeasonController) },
            { StoolballRouteType.SeasonMap, typeof(SeasonMapController) },
            { StoolballRouteType.SeasonActions, typeof(SeasonActionsController) },
            { StoolballRouteType.CreateSeason, typeof(CreateSeasonController) },
            { StoolballRouteType.EditSeason, typeof(EditSeasonController) },
            { StoolballRouteType.EditSeasonResultsTable, typeof(EditSeasonResultsTableController) },
            { StoolballRouteType.EditSeasonTeams, typeof(EditSeasonTeamsController) },
            { StoolballRouteType.DeleteSeason, typeof(DeleteSeasonController) },
            { StoolballRouteType.MatchesForSeason, typeof(MatchesForSeasonController) },
            { StoolballRouteType.SeasonResultsTable, typeof(SeasonResultsTableController) },
            { StoolballRouteType.Matches, typeof(MatchesController) },
            { StoolballRouteType.MatchesCalendar, typeof(MatchesCalendarController) },
            { StoolballRouteType.MatchesRss, typeof(MatchesRssController) },
            { StoolballRouteType.Match, typeof(MatchController) },
            { StoolballRouteType.MatchActions, typeof(MatchActionsController) },
            { StoolballRouteType.CreateTrainingSession, typeof(CreateTrainingSessionController) },
            { StoolballRouteType.CreateFriendlyMatch, typeof(CreateFriendlyMatchController) },
            { StoolballRouteType.CreateKnockoutMatch, typeof(CreateKnockoutMatchController) },
            { StoolballRouteType.CreateLeagueMatch, typeof(CreateLeagueMatchController) },
            //{ StoolballRouteType.EditFriendlyMatch, typeof(EditFriendlyMatchController) },
            //{ StoolballRouteType.EditLeagueMatch, typeof(EditLeagueMatchController) },
            //{ StoolballRouteType.EditKnockoutMatch, typeof(EditKnockoutMatchController) },
            //{ StoolballRouteType.EditTrainingSession, typeof(EditTrainingSessionController) },
            //{ StoolballRouteType.EditMatchFormat, typeof(EditMatchFormatController) },
            //{ StoolballRouteType.EditStartOfPlay, typeof(EditStartOfPlayController) },
            //{ StoolballRouteType.EditBattingScorecard, typeof(EditBattingScorecardController) },
            //{ StoolballRouteType.EditBowlingScorecard, typeof(EditBowlingScorecardController) },
            //{ StoolballRouteType.EditCloseOfPlay, typeof(EditCloseOfPlayController) },
            //{ StoolballRouteType.DeleteMatch, typeof(DeleteMatchController) },
            { StoolballRouteType.Tournaments, typeof(TournamentsController) },
            { StoolballRouteType.TournamentsRss, typeof(TournamentsRssController) },
            { StoolballRouteType.Tournament, typeof(TournamentController) },
            { StoolballRouteType.TournamentActions, typeof(TournamentActionsController) },
            { StoolballRouteType.CreateTournament, typeof(CreateTournamentController) },
            //{ StoolballRouteType.EditTournament, typeof(EditTournamentController) },
            //{ StoolballRouteType.EditTournamentMatches, typeof(EditTournamentMatchesController) },
            //{ StoolballRouteType.EditTournamentTeams, typeof(EditTournamentTeamsController) },
            //{ StoolballRouteType.EditTournamentSeasons, typeof(EditTournamentSeasonsController) },
            //{ StoolballRouteType.DeleteTournament, typeof(DeleteTournamentController) },
            //{ StoolballRouteType.Statistics, typeof(StatisticsController) },
            //{ StoolballRouteType.EditStatistics, typeof(EditStatisticsController) },
            //{ StoolballRouteType.Player, typeof(PlayerController) },
            //{ StoolballRouteType.PlayerBowling, typeof(PlayerBowlingController) },
            //{ StoolballRouteType.PlayerFielding, typeof(PlayerFieldingController) },
            { StoolballRouteType.PlayersForTeam, typeof(PlayersForTeamController) },
            { StoolballRouteType.ClubStatistics, typeof(ClubStatisticsController) },
            { StoolballRouteType.TeamStatistics, typeof(TeamStatisticsController) },
            { StoolballRouteType.MatchLocationStatistics, typeof(MatchLocationStatisticsController) },
            { StoolballRouteType.CompetitionStatistics, typeof(CompetitionStatisticsController) },
            { StoolballRouteType.SeasonStatistics, typeof(SeasonStatisticsController) },
            //{ StoolballRouteType.IndividualScores, typeof(IndividualScoresController) },
            //{ StoolballRouteType.BowlingFigures, typeof(BowlingFiguresController) },
            //{ StoolballRouteType.Catches, typeof(CatchesController) },
            //{ StoolballRouteType.RunOuts, typeof(RunOutsController) },
            //{ StoolballRouteType.MostRuns, typeof(MostRunsController) },
            //{ StoolballRouteType.MostWickets, typeof(MostWicketsController) },
            //{ StoolballRouteType.MostCatches, typeof(MostCatchesController) },
            //{ StoolballRouteType.MostRunOuts, typeof(MostRunOutsController) },
            //{ StoolballRouteType.BattingAverage, typeof(BattingAverageController) },
            //{ StoolballRouteType.BowlingAverage, typeof(BowlingAverageController) },
            //{ StoolballRouteType.EconomyRate, typeof(EconomyRateController) },
            //{ StoolballRouteType.BattingStrikeRate, typeof(BattingStrikeRateController) },
            //{ StoolballRouteType.BowlingStrikeRate, typeof(BowlingStrikeRateController) },
            { StoolballRouteType.Schools, typeof(SchoolsController) }
        };

        public Type MapRouteTypeToController(StoolballRouteType routeType)
        {
            return _supportedControllers[routeType];
        }
    }
}