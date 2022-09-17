using System;
using System.Collections.Generic;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.UnitTests.Statistics
{
    public class PlayerTests
    {
        [Fact]
        public void Player_name_orders_by_total_matches_aggregating_duplicates()
        {
            var player = new Player
            {
                PlayerIdentities = new List<PlayerIdentity>
                {
                    new PlayerIdentity {
                        PlayerIdentityName = "Name One",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "Name Two",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "Name Two",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date
                    }
                }
            };

            var preferredName = player.PlayerName();
            var alternativeNames = player.AlternativeNames();

            Assert.Equal("Name Two", preferredName);
            Assert.Single(alternativeNames);
            Assert.Equal("Name One", alternativeNames[0]);
        }

        [Fact]
        public void Player_name_prefers_most_recent_player()
        {
            var player = new Player
            {
                PlayerIdentities = new List<PlayerIdentity>
                {
                    new PlayerIdentity {
                        PlayerIdentityName = "Name One",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-1)
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "Name Two",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date
                    }
                }
            };

            var preferredName = player.PlayerName();
            var alternativeNames = player.AlternativeNames();

            Assert.Equal("Name Two", preferredName);
            Assert.Single(alternativeNames);
            Assert.Equal("Name One", alternativeNames[0]);
        }

        [Theory]
        [InlineData("Name Complete")]
        [InlineData("Name von Complete")]
        public void Player_name_prefers_complete_names(string completeName)
        {
            var player = new Player
            {
                PlayerIdentities = new List<PlayerIdentity>
                {
                    new PlayerIdentity {
                        PlayerIdentityName = "Name",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "Name A",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = completeName,
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date
                    }
                }
            };

            var preferredName = player.PlayerName();
            var alternativeNames = player.AlternativeNames();

            Assert.Equal(completeName, preferredName);
            Assert.Equal(2, alternativeNames.Count);
            Assert.Equal("Name A", alternativeNames[0]);
            Assert.Equal("Name", alternativeNames[1]);
        }

        [Fact]
        public void Player_name_orders_each_group_by_total_matches_boosting_recent_players()
        {
            var player = new Player
            {
                PlayerIdentities = new List<PlayerIdentity>
                {
                    new PlayerIdentity {
                        PlayerIdentityName = "Name One",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-5) // Fifth most recent, should boost to 2 matches * 1 weight = 2
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "Name Two",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-3) // Third most recent, should boost to 2 matches * 3 weight = 6
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "Name Three",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-1) // Most recent, should boost to 2 matches * 5 weight = 10
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "Name Four",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-2) // Second most recent, should boost to 2 matches * 4 weight = 8
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "Name Five",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-4) // Fourth most recent, should boost to 2 matches * 2 weight = 4
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "Name A",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-5) // Fifth most recent, should boost to 2 matches * 1 weight = 2
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "Name B",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-3) // Third most recent, should boost to 2 matches * 3 weight = 6
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "Name C",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-1) // Most recent, should boost to 2 matches * 5 weight = 10
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "Name D",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-2) // Second most recent, should boost to 2 matches * 4 weight = 8
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "Name E",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-4) // Fourth most recent, should boost to 2 matches * 2 weight = 4
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "NameA",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-5) // Fifth most recent, should boost to 2 matches * 1 weight = 2
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "NameB",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-3) // Third most recent, should boost to 2 matches * 3 weight = 6
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "NameC",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-1) // Most recent, should boost to 2 matches * 5 weight = 10
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "NameD",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-2) // Second most recent, should boost to 2 matches * 4 weight = 8
                    },
                    new PlayerIdentity {
                        PlayerIdentityName = "NameE",
                        TotalMatches = 2,
                        LastPlayed = DateTimeOffset.Now.Date.AddDays(-4) // Fourth most recent, should boost to 2 matches * 2 weight = 4
                    }
                }
            };

            var preferredName = player.PlayerName();
            var alternativeNames = player.AlternativeNames();

            Assert.Equal("Name Three", preferredName);
            Assert.Equal(14, alternativeNames.Count);
            Assert.Equal("Name Four", alternativeNames[0]);
            Assert.Equal("Name Two", alternativeNames[1]);
            Assert.Equal("Name Five", alternativeNames[2]);
            Assert.Equal("Name One", alternativeNames[3]);
            Assert.Equal("Name C", alternativeNames[4]);
            Assert.Equal("Name D", alternativeNames[5]);
            Assert.Equal("Name B", alternativeNames[6]);
            Assert.Equal("Name E", alternativeNames[7]);
            Assert.Equal("Name A", alternativeNames[8]);
            Assert.Equal("NameC", alternativeNames[9]);
            Assert.Equal("NameD", alternativeNames[10]);
            Assert.Equal("NameB", alternativeNames[11]);
            Assert.Equal("NameE", alternativeNames[12]);
            Assert.Equal("NameA", alternativeNames[13]);
        }
    }
}
