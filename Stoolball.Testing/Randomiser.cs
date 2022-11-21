using System;

namespace Stoolball.Testing
{
    internal class Randomiser
    {
        private readonly Random _random;

        internal Randomiser(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }
        internal bool FiftyFiftyChance()
        {
            return _random.Next(2) == 0;
        }

        internal bool OneInFourChance()
        {
            return _random.Next(4) != 0;
        }

        internal int PositiveIntegerLessThan(int maxValue)
        {
            return _random.Next(maxValue);
        }

        internal int Between(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue + 1);
        }
    }
}
