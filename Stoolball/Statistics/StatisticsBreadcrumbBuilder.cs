using System;
using System.Collections.Generic;
using Stoolball.Navigation;

namespace Stoolball.Statistics
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
                breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Statistics, Url = new Uri(Constants.Pages.StatisticsUrl, UriKind.Relative) });
                breadcrumbs.Add(new Breadcrumb { Name = filter.Player.PlayerName(), Url = new Uri(filter.Player.PlayerRoute, UriKind.Relative) });
            }
            else if (filter.Club != null)
            {
                breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
                breadcrumbs.Add(new Breadcrumb { Name = filter.Club.ClubName, Url = new Uri(filter.Club.ClubRoute, UriKind.Relative) });
            }
            else if (filter.Team != null)
            {
                breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Teams, Url = new Uri(Constants.Pages.TeamsUrl, UriKind.Relative) });
                if (filter.Team.Club != null)
                {
                    breadcrumbs.Add(new Breadcrumb { Name = filter.Team.Club.ClubName, Url = new Uri(filter.Team.Club.ClubRoute, UriKind.Relative) });
                }
                breadcrumbs.Add(new Breadcrumb { Name = filter.Team.TeamName, Url = new Uri(filter.Team.TeamRoute, UriKind.Relative) });
            }
            else if (filter.MatchLocation != null)
            {
                breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.MatchLocations, Url = new Uri(Constants.Pages.MatchLocationsUrl, UriKind.Relative) });
                breadcrumbs.Add(new Breadcrumb { Name = filter.MatchLocation.NameAndLocalityOrTownIfDifferent(), Url = new Uri(filter.MatchLocation.MatchLocationRoute, UriKind.Relative) });
            }
            else if (filter.Competition != null)
            {
                breadcrumbs.Add(new Breadcrumb { Name = Constants.Pages.Competitions, Url = new Uri(Constants.Pages.CompetitionsUrl, UriKind.Relative) });
                breadcrumbs.Add(new Breadcrumb { Name = filter.Competition.CompetitionName, Url = new Uri(filter.Competition.CompetitionRoute, UriKind.Relative) });
            }
            else if (filter.Season != null)
            {
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