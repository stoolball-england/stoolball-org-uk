using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Web.Models;

namespace Stoolball.Web.Matches.Models
{
    public interface IEditMatchViewModel : IHasViewMetadata
    {
        Guid? AwayTeamId { get; set; }
        string? HomeTeamName { get; set; }
        string? AwayTeamName { get; set; }
        Guid? HomeTeamId { get; set; }
        Match? Match { get; set; }
        DateTimeOffset? MatchDate { get; set; }
        Guid? MatchLocationId { get; set; }
        string? MatchLocationName { get; set; }
        List<SelectListItem> PossibleSeasons { get; }
        List<SelectListItem> PossibleHomeTeams { get; }
        List<SelectListItem> PossibleAwayTeams { get; }
        Season? Season { get; set; }
        string? SeasonFullName { get; set; }
        DateTimeOffset? StartTime { get; set; }
        Team? Team { get; set; }
    }
}