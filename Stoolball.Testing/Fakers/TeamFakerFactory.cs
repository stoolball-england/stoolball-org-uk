using System;
using System.Collections.Generic;
using Bogus;
using Humanizer;
using Stoolball.Teams;

namespace Stoolball.Testing.Fakers
{
    public class TeamFakerFactory : IFakerFactory<Team>
    {
        public Faker<Team> Create()
        {
            return new Faker<Team>()
                    .RuleFor(x => x.TeamId, () => Guid.NewGuid())
                    .RuleFor(x => x.TeamName, faker => faker.Address.City() + " " + faker.Random.ListItem<string>(new List<string> { "Tigers", "Bears", "Wolves", "Eagles", "Dolphins", "Stars", "Rockets", "Badgers", "Foxes", "Wildcats" }))
                    .RuleFor(x => x.MemberGroupKey, () => Guid.NewGuid())
                    .RuleFor(x => x.MemberGroupName, (faker, team) => team.TeamName + " owners")
                    .RuleFor(x => x.TeamRoute, (faker, team) => $"/teams/{team.TeamName.Kebaberize()}-{team.TeamId}");
        }
    }
}
