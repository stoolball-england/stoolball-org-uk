using System;

namespace Stoolball.Data.SqlServer.IntegrationTests
{
    public static class DecimalExtensions
    {
        public static decimal AccurateToTwoDecimalPlaces(this decimal number)
        {
            return decimal.Round(number, 2, MidpointRounding.AwayFromZero);
        }
    }
}
