using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using Stoolball.Web.Matches.Models;

namespace Stoolball.Web.Matches
{
    public class EditMatchHelper : IEditMatchHelper
    {
        private readonly ISeasonDataSource _seasonDataSource;

        public EditMatchHelper(ISeasonDataSource seasonDataSource)
        {
            _seasonDataSource = seasonDataSource ?? throw new ArgumentNullException(nameof(seasonDataSource));
        }

        public List<SelectListItem> PossibleSeasonsAsListItems(IEnumerable<Season> seasons)
        {
            if (seasons != null && seasons.Any())
            {
                return seasons.Select(x => new SelectListItem { Text = x.SeasonFullName(), Value = x.SeasonId!.Value.ToString() }).ToList();
            }
            else
            {
                return new List<SelectListItem>();
            }
        }

        public List<SelectListItem> PossibleTeamsAsListItems(IEnumerable<TeamInSeason> teams)
        {
            if (teams != null && teams.Any())
            {
                var listItems = teams.Select(x => new SelectListItem { Text = x.Team.TeamName, Value = x.Team.TeamId!.Value.ToString() }).ToList();
                listItems.Sort(new TeamSelectListItemComparer(null));
                return listItems;
            }
            else
            {
                return new List<SelectListItem>();
            }
        }

        public async Task ConfigureModelPossibleTeams(IEditMatchViewModel model, IEnumerable<Season> possibleSeasons)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (model.Team == null)
            {
                throw new ArgumentException($"{nameof(model.Team)} cannot be null");
            }

            if (possibleSeasons is null)
            {
                throw new ArgumentNullException(nameof(possibleSeasons));
            }

            var possibleTeams = new List<Team>();
            foreach (var season in possibleSeasons)
            {
                var teamsInSeason = (await _seasonDataSource.ReadSeasonByRoute(season.SeasonRoute, true))?.Teams.Where(x => x.WithdrawnDate == null).Select(x => x.Team);
                if (teamsInSeason != null)
                {
                    possibleTeams.AddRange(teamsInSeason);
                }
            }
            model.PossibleHomeTeams.AddRange(possibleTeams.OfType<Team>().Distinct(new TeamEqualityComparer()).Select(x => new SelectListItem { Text = x.TeamName, Value = x.TeamId!.Value.ToString() }));
            model.PossibleHomeTeams.Sort(new TeamSelectListItemComparer(model.Team.TeamId));
            model.PossibleAwayTeams.AddRange(possibleTeams.OfType<Team>().Distinct(new TeamEqualityComparer()).Select(x => new SelectListItem { Text = x.TeamName, Value = x.TeamId!.Value.ToString() }));
            model.PossibleAwayTeams.Sort(new TeamSelectListItemComparer(model.Team.TeamId));
        }

        public void ConfigureModelFromRequestData(IEditMatchViewModel model, IFormCollection formData, ModelStateDictionary modelState)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (model.Match is null)
            {
                throw new ArgumentException($"{nameof(model.Match)} cannot be null");
            }

            if (formData is null)
            {
                throw new ArgumentNullException(nameof(formData));
            }

            if (modelState is null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            // get this from the form instead of via modelbinding so that HTML can be allowed
            model.Match.MatchNotes = formData["Match.MatchNotes"];

            if (!string.IsNullOrEmpty(formData["MatchName"]))
            {
                model.Match.MatchName = formData["MatchName"];
            }

            if (!string.IsNullOrEmpty(formData["MatchDate"]) && DateTimeOffset.TryParse(formData["MatchDate"], out var parsedDate))
            {
                model.MatchDate = parsedDate;
                model.Match.StartTime = model.MatchDate.Value;

                if (!string.IsNullOrEmpty(formData["StartTime"]))
                {
                    if (DateTimeOffset.TryParse(formData["StartTime"], out var parsedTime))
                    {
                        model.StartTime = parsedTime;
                        model.Match.StartTime = model.Match.StartTime.Add(model.StartTime.Value.TimeOfDay);
                        model.Match.StartTimeIsKnown = true;
                    }
                    else
                    {
                        // This may be seen in browsers that don't support <input type="time" />, mainly Safari.
                        // Each browser that supports <input type="time" /> may have a very different interface so don't advertise
                        // this format up-front as it could confuse the majority. Instead, only reveal it here.
                        modelState.AddModelError("StartTime", "Enter a time in 24-hour HH:MM format.");
                    }
                }
                else
                {
                    // If no start time specified, use a typical one but don't show it
                    model.Match.StartTime.AddHours(19);
                    model.Match.StartTimeIsKnown = false;
                }
            }
            else
            {
                // This may be seen in browsers that don't support <input type="date" />, mainly Safari. 
                // This is the format <input type="date" /> expects and posts, so we have to repopulate the field in this format,
                // so although this code _can_ parse other formats we don't advertise that. We also don't want YYYY-MM-DD in 
                // the field label as it could confuse the majority, so only reveal it here.
                modelState.AddModelError("MatchDate", "Enter a date in YYYY-MM-DD format.");
            }

            if (!string.IsNullOrEmpty(formData["HomeTeamId"]))
            {
                model.HomeTeamId = new Guid(formData["HomeTeamId"]!);
                model.HomeTeamName = formData["HomeTeamName"];
                model.Match.Teams.Add(new TeamInMatch
                {
                    Team = new Team { TeamId = model.HomeTeamId, TeamName = model.HomeTeamName },
                    TeamRole = TeamRole.Home
                });
            }
            if (!string.IsNullOrEmpty(formData["AwayTeamId"]))
            {
                model.AwayTeamId = new Guid(formData["AwayTeamId"]!);
                model.AwayTeamName = formData["AwayTeamName"];
                model.Match.Teams.Add(new TeamInMatch
                {
                    Team = new Team { TeamId = model.AwayTeamId, TeamName = model.AwayTeamName },
                    TeamRole = TeamRole.Away
                });
            }
            if (!string.IsNullOrEmpty(formData["MatchLocationId"]))
            {
                model.MatchLocationId = new Guid(formData["MatchLocationId"]!);
                model.MatchLocationName = formData["MatchLocationName"];
                model.Match.MatchLocation = new MatchLocation
                {
                    MatchLocationId = model.MatchLocationId
                };
            }
            model.SeasonFullName = formData["SeasonFullName"];
        }


        public void ConfigureAddMatchModelMetadata(IEditMatchViewModel model)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (model.Match == null)
            {
                throw new ArgumentException($"{nameof(model.Match)} cannot be null");
            }

            if (model.Team != null)
            {
                model.Metadata.PageTitle = $"Add a {model.Match.MatchType.Humanize(LetterCasing.LowerCase)} for {model.Team.TeamName}";
            }
            else if (model.Season != null)
            {
                model.Metadata.PageTitle = $"Add a {model.Match.MatchType.Humanize(LetterCasing.LowerCase)} in the {model.Season.SeasonFullName()}";
            }
        }
    }
}