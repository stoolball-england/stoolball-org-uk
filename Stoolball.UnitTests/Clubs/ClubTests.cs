using Stoolball.Clubs;
using Stoolball.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Stoolball.UnitTests.Clubs
{
    public class ClubTests : ValidationBaseTest
    {
        [Fact]
        public void ClubName_is_required()
        {
            var club = new Club();

            Assert.Contains(ValidateModel(club),
                v => v.MemberNames.Contains(nameof(Club.ClubName)) &&
                     v.ErrorMessage.Contains("is required", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void Club_without_teams_has_expected_description()
        {
            var club = new Club { ClubName = "Example" };

            var description = club.Description();

            Assert.Equal("Example is a stoolball club, but it does not have any active teams.", description);
        }

        [Fact]
        public void Club_with_one_team_has_expected_description()
        {
            var club = new Club { ClubName = "Example", Teams = new List<Team> { new Team { TeamName = "Team A" } } };

            var description = club.Description();

            Assert.Equal("Example is a stoolball club with 1 team: Team A.", description);
        }


        [Fact]
        public void Club_with_multiple_teams_has_expected_description()
        {
            var club = new Club
            {
                ClubName = "Example",
                Teams = new List<Team> {
                new Team { TeamName = "Team A" },
                new Team { TeamName = "Team B" }
            }
            };

            var description = club.Description();

            Assert.Equal("Example is a stoolball club with 2 teams: Team A and Team B.", description);
        }
    }
}
