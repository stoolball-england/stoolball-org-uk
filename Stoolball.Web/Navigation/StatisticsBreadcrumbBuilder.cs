using System;
using System.Collections.Generic;
using Stoolball.Statistics;

namespace Stoolball.Web.Navigation
{
    public class StatisticsBreadcrumbBuilder : IStatisticsBreadcrumbBuilder
    {
        public void BuildBreadcrumbs(List<Breadcrumb> breadcrumbs, StatisticsFilter filter)
        {
            if (breadcrumbs is null)
            {
                throw new ArgumentNullException(nameof(breadcrumbs));
            }

            if (filter is null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (filter.Player != null)
            {
                if (string.IsNullOrEmpty(filter.Player.PlayerRoute)) { throw new ArgumentException($"When {nameof(filter)}.{nameof(filter.Player)} is not null, {nameof(filter)}.{nameof(filter.Player)}.{nameof(filter.Player.PlayerRoute)} must be supplied."); }
                breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Statistics, Url = new Uri(Constants.Pages.StatisticsUrl, UriKind.Relative) });
                breadcrumbs.Add(new Breadcrumb { Name = filter.Player.PlayerName(), Url = new Uri(filter.Player.PlayerRoute, UriKind.Relative) });
            }
            else if (filter.Club != null)
            {
                if (string.IsNullOrEmpty(filter.Club.ClubRoute)) { throw new ArgumentException($"When {nameof(filter)}.{nameof(filter.Club)} is not null, {nameof(filter)}.{nameof(filter.Club)}.{nameof(filter.Club.ClubRoute)} must be supplied."); }
                breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
                breadcrumbs.Add(new Breadcrumb { Name = filter.Club.ClubName, Url = new Uri(filter.Club.ClubRoute, UriKind.Relative) });
            }
            else if (filter.Team != null)
            {
                breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
                if (filter.Team.Club != null)
                {
                    if (string.IsNullOrEmpty(filter.Team.Club.ClubRoute)) { throw new ArgumentException($"When {nameof(filter)}.{nameof(filter.Team)}.{nameof(filter.Club)} is not null, {nameof(filter)}.{nameof(filter.Team)}.{nameof(filter.Club)}.{nameof(filter.Club.ClubRoute)} must be supplied."); }
                    breadcrumbs.Add(new Breadcrumb { Name = filter.Team.Club.ClubName, Url = new Uri(filter.Team.Club.ClubRoute, UriKind.Relative) });
                }
                if (string.IsNullOrEmpty(filter.Team.TeamRoute)) { throw new ArgumentException($"When {nameof(filter)}.{nameof(filter.Team)} is not null, {nameof(filter)}.{nameof(filter.Team)}.{nameof(filter.Team.TeamRoute)} must be supplied."); }
                breadcrumbs.Add(new Breadcrumb { Name = filter.Team.TeamName, Url = new Uri(filter.Team.TeamRoute, UriKind.Relative) });
            }
            else if (filter.MatchLocation != null)
            {
                if (string.IsNullOrEmpty(filter.MatchLocation.MatchLocationRoute)) { throw new ArgumentException($"When {nameof(filter)}.{nameof(filter.MatchLocation)} is not null, {nameof(filter)}.{nameof(filter.MatchLocation)}.{nameof(filter.MatchLocation.MatchLocationRoute)} must be supplied."); }
                breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });
                breadcrumbs.Add(new Breadcrumb { Name = filter.MatchLocation.NameAndLocalityOrTownIfDifferent(), Url = new Uri(filter.MatchLocation.MatchLocationRoute, UriKind.Relative) });
            }
            else if (filter.Competition != null)
            {
                if (string.IsNullOrEmpty(filter.Competition.CompetitionRoute)) { throw new ArgumentException($"When {nameof(filter)}.{nameof(filter.Competition)} is not null, {nameof(filter)}.{nameof(filter.Competition)}.{nameof(filter.Competition.CompetitionRoute)} must be supplied."); }
                breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                breadcrumbs.Add(new Breadcrumb { Name = filter.Competition.CompetitionName, Url = new Uri(filter.Competition.CompetitionRoute, UriKind.Relative) });
            }
            else if (filter.Season != null)
            {
                if (string.IsNullOrEmpty(filter.Season.Competition?.CompetitionRoute)) { throw new ArgumentException($"When {nameof(filter)}.{nameof(filter.Season)} is not null, {nameof(filter)}.{nameof(filter.Season)}.{nameof(filter.Competition)}.{nameof(filter.Competition.CompetitionRoute)} must be supplied."); }
                if (string.IsNullOrEmpty(filter.Season.SeasonRoute)) { throw new ArgumentException($"When {nameof(filter)}.{nameof(filter.Season)} is not null, {nameof(filter)}.{nameof(filter.Season)}.{nameof(filter.Season.SeasonRoute)} must be supplied."); }
                breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                breadcrumbs.Add(new Breadcrumb { Name = filter.Season.Competition.CompetitionName, Url = new Uri(filter.Season.Competition.CompetitionRoute, UriKind.Relative) });
                breadcrumbs.Add(new Breadcrumb { Name = filter.Season.SeasonName(), Url = new Uri(filter.Season.SeasonRoute, UriKind.Relative) });
            }
            else
            {
                breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Statistics, Url = new Uri(Constants.Pages.StatisticsUrl, UriKind.Relative) });
            }
        }
    }
}