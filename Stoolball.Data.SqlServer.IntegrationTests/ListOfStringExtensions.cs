using System;
using System.Collections.Generic;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    internal static class ListOfStringExtensions
    {
        internal static List<string> ChangeCaseAndSometimesTrimOneEnd(this List<string> strings)
        {
            var randomiser = new Random();
            var minimumLength = 8; // We want it to remain unique enough that it doesn't match other fields
            for (var i = 0; i < strings.Count; i++)
            {
                // maybe trim some characters from the start or end because we want partial searches to work
                var howManyToTrim = randomiser.Next(0, 4);
                var trimStart = randomiser.Next(0, 2) == 0;
                if (trimStart && strings[i].Length > (howManyToTrim + minimumLength))
                {
                    strings[i] = strings[i].Substring(howManyToTrim);
                }
                else if (!trimStart && strings[i].Length > (howManyToTrim + minimumLength))
                {
                    strings[i] = strings[i].Substring(0, strings[i].Length - howManyToTrim);
                }

                // change the case to prove it's case insensitive
                strings[i] = strings[i] == strings[i].ToUpperInvariant() ? strings[i].ToLowerInvariant() : strings[i].ToUpperInvariant();
            }
            return strings;
        }
    }
}