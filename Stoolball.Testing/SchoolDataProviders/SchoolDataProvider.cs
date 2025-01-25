using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Stoolball.MatchLocations;
using Stoolball.Schools;
using Stoolball.Teams;
using Stoolball.Testing.Fakers;
using Stoolball.Testing.PlayerDataProviders;

namespace Stoolball.Testing.SchoolDataProviders
{
    internal class SchoolDataProvider(IFakerFactory<School> _schoolFakerFactory, IFakerFactory<Team> _teamFakerFactory, IFakerFactory<MatchLocation> _matchLocationFakerFactory) : BaseSchoolDataProvider
    {
        internal override IEnumerable<School> CreateSchools()
        {
            // Create schools data, with extra separate teams and locations
            var schoolFaker = _schoolFakerFactory.Create();
            var schools = schoolFaker.Generate(20);

            CreateSchoolTeamsForSchools(schools, _teamFakerFactory.Create, _matchLocationFakerFactory.Create);

            return schools;
        }

        private void CreateSchoolTeamsForSchools(List<School> schools, Func<Faker<Team>> teamFakerMaker, Func<Faker<MatchLocation>> locationFakerMaker)
        {
            var schoolTeamFaker = teamFakerMaker()
                .RuleFor(x => x.TeamType, faker => faker.Random.ListItem<TeamType>(new List<TeamType> { TeamType.SchoolAgeGroup, TeamType.SchoolClub, TeamType.SchoolOther }));
            var locationFaker = locationFakerMaker();

            for (var i = 0; i < schools.Count; i++)
            {
                // First 3 schools have no teams therefore the school should be inactive.
                if (i < 3)
                {
                    schools[i].UntilYear = 2019;
                    continue;
                }

                // Then even indexes get one team, odd get multiple.
                schools[i].Teams = schoolTeamFaker.Generate((i % 2) + 1);
                schools[i].Teams.ForEach(t => t.School = schools[i]);

                // Up to 7 they don't get a match location. Above that they do.
                // At index 9 the school with two teams gets the same location twice. 
                // At index 11 the school gets multiple teams with different locations AND multiple locations for a team.
                if (i == 9)
                {
                    // handle special case
                    var location = locationFaker.Generate(1).First();
                    schools[i].Teams.ForEach(x =>
                    {
                        x.MatchLocations.Add(location);
                        location.Teams.Add(x);
                    });
                }
                else if (i == 11)
                {
                    // handle special case
                    var locations1and2 = locationFaker.Generate(2);
                    schools[i].Teams[0].MatchLocations.AddRange(locations1and2);
                    locations1and2[0].Teams.Add(schools[i].Teams[0]);
                    locations1and2[1].Teams.Add(schools[i].Teams[0]);

                    var location3 = locationFaker.Generate(1).First();
                    schools[i].Teams[1].MatchLocations.Add(location3);
                    location3.Teams.Add(schools[i].Teams[1]);
                }
                else if (i > 7)
                {
                    // add a match location to each team
                    schools[i].Teams.ForEach(x =>
                    {
                        var location = locationFaker.Generate(1).First();
                        x.MatchLocations.Add(location);
                        location.Teams.Add(x);
                    });
                }

                // Up to 11, all teams are active.
                // Above 11, one team in multiple is inactive.
                // Above 15, both teams are inactive therefore the school should be inactive.
                if (i > 15 && schools[i].Teams.Count > 1)
                {
                    schools[i].Teams.ForEach(t => t.UntilYear = DateTimeOffset.UtcNow.AddYears(-1).Year);
                }
                else if (i > 11 && schools[i].Teams.Count > 1)
                {
                    schools[i].Teams[0].UntilYear = DateTimeOffset.UtcNow.AddYears(-2).Year;
                }
            }
        }
    }
}
