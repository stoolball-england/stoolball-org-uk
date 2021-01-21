using Stoolball.Statistics;
using Xunit;

namespace Stoolball.UnitTests.Statistics
{
    public class BowlingFiguresTests
    {
        [Fact]
        public void BowlingAverage_is_null_when_wickets_is_0()
        {
            var bowlingFigures = new BowlingFigures
            {
                RunsConceded = 10,
                Wickets = 0
            };

            var result = bowlingFigures.BowlingAverage();

            Assert.Null(result);
        }

        [Fact]
        public void BowlingAverage_is_correct_for_integers()
        {
            var bowlingFigures = new BowlingFigures
            {
                RunsConceded = 30,
                Wickets = 2
            };

            var result = bowlingFigures.BowlingAverage();

            Assert.Equal(15, result);
        }

        [Fact]
        public void BowlingAverage_is_correct_to_two_decimal_places_when_returning_fractions()
        {
            var bowlingFigures = new BowlingFigures
            {
                RunsConceded = 32,
                Wickets = 3
            };

            var result = bowlingFigures.BowlingAverage();

            Assert.Equal(10.67M, result);
        }

        [Fact]
        public void BowlingEconomy_is_null_when_overs_is_0()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 0,
                RunsConceded = 10
            };

            var result = bowlingFigures.BowlingEconomy();

            Assert.Null(result);
        }

        [Fact]
        public void BowlingEconomy_is_correct_for_integers()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 3,
                RunsConceded = 30
            };

            var result = bowlingFigures.BowlingEconomy();

            Assert.Equal(10, result);
        }

        [Fact]
        public void BowlingEconomy_is_correct_to_two_decimal_places_when_returning_fractions()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 4,
                RunsConceded = 41
            };

            var result = bowlingFigures.BowlingEconomy();

            Assert.Equal(10.25M, result);
        }

        [Fact]
        public void BowlingEconomy_is_correct_for_incomplete_overs()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 3.4M,
                RunsConceded = 28
            };

            var result = bowlingFigures.BowlingEconomy();

            Assert.Equal(8, result);
        }

        [Fact]
        public void BowlingStrikeRate_is_null_when_overs_is_0()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 0,
                Wickets = 5
            };

            var result = bowlingFigures.BowlingStrikeRate();

            Assert.Null(result);
        }

        [Fact]
        public void BowlingStrikeRate_is_null_when_wickets_is_0()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 5,
                Wickets = 0
            };

            var result = bowlingFigures.BowlingStrikeRate();

            Assert.Null(result);
        }

        [Fact]
        public void BowlingStrikeRate_is_correct_for_integers()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 2,
                Wickets = 2
            };

            var result = bowlingFigures.BowlingStrikeRate();

            Assert.Equal(8, result);
        }


        [Fact]
        public void BowlingStrikeRate_is_correct_for_incomplete_overs()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 2.4M,
                Wickets = 2
            };

            var result = bowlingFigures.BowlingStrikeRate();

            Assert.Equal(10, result);
        }

        [Fact]
        public void BowlingStrikeRate_is_correct_to_two_decimal_places_when_returning_fractions()
        {
            var bowlingFigures = new BowlingFigures
            {
                Overs = 10,
                Wickets = 9
            };

            var result = bowlingFigures.BowlingStrikeRate();

            Assert.Equal(8.89M, result);
        }
    }
}
