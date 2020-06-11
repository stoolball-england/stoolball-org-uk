using Stoolball.Web.Clubs;
using Stoolball.Web.Competitions;
using Stoolball.Web.Matches;
using Stoolball.Web.MatchLocations;
using Stoolball.Web.Teams;
using System;
using System.Collections.Generic;

namespace Stoolball.Web.Routing
{
    public class StoolballRouteTypeMapper : IStoolballRouteTypeMapper
    {
        private readonly Dictionary<StoolballRouteType, Type> _supportedControllers = new Dictionary<StoolballRouteType, Type> {
            { StoolballRouteType.Clubs, typeof(ClubsController) },
            { StoolballRouteType.ClubActions, typeof(ClubActionsController) },
            { StoolballRouteType.Club, typeof(ClubController) },
            { StoolballRouteType.CreateClub, typeof(CreateClubController) },
            { StoolballRouteType.EditClub, typeof(EditClubController) },
            { StoolballRouteType.DeleteClub, typeof(DeleteClubController) },
            { StoolballRouteType.MatchesForClub, typeof(MatchesForClubController) },
            { StoolballRouteType.Teams, typeof(TeamsController) },
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
            { StoolballRouteType.DeleteSeason, typeof(DeleteSeasonController) },
            { StoolballRouteType.MatchesForSeason, typeof(MatchesForSeasonController) },
            { StoolballRouteType.Match, typeof(MatchController) },
            { StoolballRouteType.DeleteMatch, typeof(DeleteMatchController) },
            { StoolballRouteType.Tournament, typeof(TournamentController) },
            { StoolballRouteType.DeleteTournament, typeof(DeleteTournamentController) }
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