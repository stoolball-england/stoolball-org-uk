﻿using System;

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
        internal bool IsEven(int i)
        {
            return i % 2 == 0;
        }

        /// <summary>
        /// Gets a random value from an enum type
        /// </summary>
        /// <typeparam name="T">The enum</typeparam>
        /// <returns>A single enum value</returns>
        internal T RandomEnum<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T)v.GetValue(_random.Next(v.Length))!;
        }
    }
}
