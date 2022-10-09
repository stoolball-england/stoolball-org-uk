using System;
using System.Text.RegularExpressions;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Schools;
using Stoolball.Teams;

namespace Stoolball
{
    public static class ComparableNameExtensions
    {
        /// <summary>
        /// Gets the version of an club's name used to sort
        /// </summary>
        /// <returns>A version of the name normalised for sorting</returns>
        public static string ComparableName(this Club club)
        {
            return StandardComparableName(club.ClubName);
        }

        /// <summary>
        /// Gets the version of an competition's name used to sort
        /// </summary>
        /// <returns>A version of the name normalised for sorting</returns>
        public static string ComparableName(this Competition competition)
        {
            return StandardComparableName(competition.CompetitionName);
        }

        /// <summary>
        /// Gets the version of an school's name used to sort
        /// </summary>
        /// <returns>A version of the name normalised for sorting</returns>
        public static string ComparableName(this School school)
        {
            return StandardComparableName(school.SchoolName);
        }

        /// <summary>
        /// Gets the version of an team's name used to sort
        /// </summary>
        /// <returns>A version of the name normalised for sorting</returns>
        public static string ComparableName(this Team team)
        {
            return StandardComparableName(team.TeamNameAndPlayerType());
        }

        /// <summary>
        /// Gets the version of an entity's name used to sort
        /// </summary>
        /// <returns>A version of the name normalised for sorting</returns>
        private static string StandardComparableName(string? name)
        {
            var comparable = name?.ToUpperInvariant() ?? string.Empty;
            if (comparable.StartsWith("THE ", StringComparison.Ordinal)) { comparable = comparable.Substring(4); }
            return Regex.Replace(comparable, "[^A-Z0-9]", string.Empty);
        }
    }
}
