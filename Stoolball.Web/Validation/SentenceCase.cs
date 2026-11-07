using System;
using System.Linq;

namespace Stoolball.Web.Validation
{
    /// <summary>
    /// Display names are written for labels, so they start with a capital letter. Use this to make one suitable for the middle of a sentence.
    /// </summary>
    public static class SentenceCase
    {
        /// <summary>
        /// Names which start with a proper noun, and so keep their capital letter in the middle of a sentence.
        /// </summary>
        private static readonly string[] _properNouns = ["Facebook", "Instagram", "Stoolball England", "Twitter"];

        /// <summary>
        /// Lower-cases the first letter of a display name, unless it starts with a proper noun, or an acronym or brand name with its own capital letters such as YouTube.
        /// </summary>
        public static string ToMidSentence(string? displayName)
        {
            if (string.IsNullOrEmpty(displayName) || !char.IsUpper(displayName[0]))
            {
                return displayName ?? string.Empty;
            }

            if (_properNouns.Any(x => StartsWithWords(displayName, x)))
            {
                return displayName;
            }

            var firstWord = displayName.Split(' ', 2)[0];
            if (firstWord.Skip(1).Any(char.IsUpper))
            {
                return displayName;
            }

            return char.ToLowerInvariant(displayName[0]) + displayName[1..];
        }

        private static bool StartsWithWords(string value, string words)
        {
            return value.StartsWith(words, StringComparison.Ordinal) &&
                (value.Length == words.Length || !char.IsLetterOrDigit(value[words.Length]));
        }
    }
}
