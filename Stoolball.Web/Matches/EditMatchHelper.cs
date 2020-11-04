using Humanizer;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.MatchLocations;
using Stoolball.Teams;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

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
                return seasons.Select(x => new SelectListItem { Text = x.SeasonFullName(), Value = x.SeasonId.Value.ToString() }).ToList();
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
                var listItems = teams.Select(x => new SelectListItem { Text = x.Team.TeamName, Value = x.Team.TeamId.Value.ToString() }).ToList();
                listItems.Sort(new TeamComparer(null));
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

            if (possibleSeasons is null)
            {
                throw new ArgumentNullException(nameof(possibleSeasons));
            }

            var possibleTeams = new List<Team>();
            foreach (var season in possibleSeasons)
            {
                var teamsInSeason = (await _seasonDataSource.ReadSeasonByRoute(season.SeasonRoute, true).ConfigureAwait(false))?.Teams.Where(x => x.WithdrawnDate == null).Select(x => x.Team);
                if (teamsInSeason != null)
                {
                    possibleTeams.AddRange(teamsInSeason);
                }
            }
            model.PossibleHomeTeams.AddRange(possibleTeams.OfType<Team>().Distinct(new TeamEqualityComparer()).Select(x => new SelectListItem { Text = x.TeamName, Value = x.TeamId.Value.ToString() }));
            model.PossibleHomeTeams.Sort(new TeamComparer(model.Team.TeamId));
            model.PossibleAwayTeams.AddRange(possibleTeams.OfType<Team>().Distinct(new TeamEqualityComparer()).Select(x => new SelectListItem { Text = x.TeamName, Value = x.TeamId.Value.ToString() }));
            model.PossibleAwayTeams.Sort(new TeamComparer(model.Team.TeamId));
        }

        public void ConfigureModelFromRequestData(IEditMatchViewModel model, NameValueCollection unvalidatedFormData, NameValueCollection formData)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (unvalidatedFormData is null)
            {
                throw new ArgumentNullException(nameof(unvalidatedFormData));
            }

            if (formData is null)
            {
                throw new ArgumentNullException(nameof(formData));
            }

            // get this from the unvalidated form instead of via modelbinding so that HTML can be allowed
            model.Match.MatchNotes = unvalidatedFormData["Match.MatchNotes"];

            if (!string.IsNullOrEmpty(formData["MatchName"]))
            {
                model.Match.MatchName = formData["MatchName"];
            }

            if (!string.IsNullOrEmpty(formData["MatchDate"]))
            {
                model.MatchDate = DateTimeOffset.Parse(formData["MatchDate"], CultureInfo.CurrentCulture);
                model.Match.StartTime = model.MatchDate.Value;
                if (!string.IsNullOrEmpty(formData["StartTime"]))
                {
                    model.StartTime = DateTimeOffset.Parse(formData["StartTime"], CultureInfo.CurrentCulture);
                    model.Match.StartTime = model.Match.StartTime.Add(model.StartTime.Value.TimeOfDay);
                    model.Match.StartTimeIsKnown = true;
                }
                else
                {
                    // If no start time specified, use a typical one but don't show it
                    model.Match.StartTime.AddHours(19);
                    model.Match.StartTimeIsKnown = false;
                }
            }
            if (!string.IsNullOrEmpty(formData["HomeTeamId"]))
            {
                model.HomeTeamId = new Guid(formData["HomeTeamId"]);
                model.HomeTeamName = formData["HomeTeamName"];
                model.Match.Teams.Add(new TeamInMatch
                {
                    Team = new Team { TeamId = model.HomeTeamId, TeamName = model.HomeTeamName },
                    TeamRole = TeamRole.Home
                });
            }
            if (!string.IsNullOrEmpty(formData["AwayTeamId"]))
            {
                model.AwayTeamId = new Guid(formData["AwayTeamId"]);
                model.AwayTeamName = formData["AwayTeamName"];
                model.Match.Teams.Add(new TeamInMatch
                {
                    Team = new Team { TeamId = model.AwayTeamId, TeamName = model.AwayTeamName },
                    TeamRole = TeamRole.Away
                });
            }
            if (!string.IsNullOrEmpty(formData["MatchLocationId"]))
            {
                model.MatchLocationId = new Guid(formData["MatchLocationId"]);
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