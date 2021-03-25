using System;
using System.Collections.Generic;
using Stoolball.Web.Clubs;
using Stoolball.Web.Competitions;
using Stoolball.Web.Matches;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.Statistics;
using Stoolball.Web.Teams;

namespace Stoolball.Web.Routing
{
    public class StoolballRouteTypeMapper : IStoolballRouteTypeMapper
    {
        private readonly Dictionary<StoolballRouteType, Type> _supportedControllers = new Dictionary<StoolballRouteType, Type> {
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
            { StoolballRouteType.SeasonActions, typeof(SeasonActionsController) },
            { StoolballRouteType.CreateSeason, typeof(CreateSeasonController) },
            { StoolballRouteType.EditSeason, typeof(EditSeasonController) },
            { StoolballRouteType.EditSeasonResultsTable, typeof(EditSeasonResultsTableController) },
            { StoolballRouteType.EditSeasonTeams, typeof(EditSeasonTeamsController) },
            { StoolballRouteType.DeleteSeason, typeof(DeleteSeasonController) },
            { StoolballRouteType.MatchesForSeason, typeof(MatchesForSeasonController) },
            { StoolballRouteType.SeasonResultsTable, typeof(SeasonResultsTableController) },
            { StoolballRouteType.Matches, typeof(MatchesController) },
            { StoolballRouteType.MatchesRss, typeof(MatchesRssController) },
            { StoolballRouteType.Match, typeof(MatchController) },
            { StoolballRouteType.MatchActions, typeof(MatchActionsController) },
            { StoolballRouteType.CreateTrainingSession, typeof(CreateTrainingSessionController) },
            { StoolballRouteType.CreateFriendlyMatch, typeof(CreateFriendlyMatchController) },
            { StoolballRouteType.CreateKnockoutMatch, typeof(CreateKnockoutMatchController) },
            { StoolballRouteType.CreateLeagueMatch, typeof(CreateLeagueMatchController) },
            { StoolballRouteType.EditFriendlyMatch, typeof(EditFriendlyMatchController) },
            { StoolballRouteType.EditLeagueMatch, typeof(EditLeagueMatchController) },
            { StoolballRouteType.EditKnockoutMatch, typeof(EditKnockoutMatchController) },
            { StoolballRouteType.EditStartOfPlay, typeof(EditStartOfPlayController) },
            { StoolballRouteType.EditBattingScorecard, typeof(EditBattingScorecardController) },
            { StoolballRouteType.EditBowlingScorecard, typeof(EditBowlingScorecardController) },
            { StoolballRouteType.EditCloseOfPlay, typeof(EditCloseOfPlayController) },
            { StoolballRouteType.DeleteMatch, typeof(DeleteMatchController) },
            { StoolballRouteType.TournamentsRss, typeof(TournamentsRssController) },
            { StoolballRouteType.Tournament, typeof(TournamentController) },
            { StoolballRouteType.TournamentActions, typeof(TournamentActionsController) },
            { StoolballRouteType.CreateTournament, typeof(CreateTournamentController) },
            { StoolballRouteType.EditTournament, typeof(EditTournamentController) },
            { StoolballRouteType.EditTournamentTeams, typeof(EditTournamentTeamsController) },
            { StoolballRouteType.DeleteTournament, typeof(DeleteTournamentController) },
            { StoolballRouteType.Statistics, typeof(StatisticsController) },
            { StoolballRouteType.EditStatistics, typeof(EditStatisticsController) },
            { StoolballRouteType.Player, typeof(PlayerController) },
            { StoolballRouteType.ClubStatistics, typeof(ClubStatisticsController) },
            { StoolballRouteType.TeamStatistics, typeof(TeamStatisticsController) },
            { StoolballRouteType.MatchLocationStatistics, typeof(MatchLocationStatisticsController) },
            { StoolballRouteType.CompetitionStatistics, typeof(CompetitionStatisticsController) },
            { StoolballRouteType.SeasonStatistics, typeof(SeasonStatisticsController) },
            { StoolballRouteType.IndividualScores, typeof(IndividualScoresController) },
            { StoolballRouteType.BowlingFigures, typeof(BowlingFiguresController) }
        };

        public Type MapRouteTypeToController(string unparsedRouteType)
        {
            if (Enum.TryParse<StoolballRouteType>(unparsedRouteType, true, out var routeType))
            {
                return _supportedControllers[routeType];
            }
            return null;
        }
    }
}