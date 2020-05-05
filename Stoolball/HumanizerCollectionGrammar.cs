using Humanizer.Localisation.CollectionFormatters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Stoolball
{
    /// <summary>
    /// Formats a collection as "one, two and three"
    /// </summary>
    /// <remarks>
    /// Adapted from the <a href="https://github.com/Humanizr/Humanizer/blob/master/src/Humanizer/Localisation/CollectionFormatters/DefaultCollectionFormatter.cs">DefaultCollectionFormatter</a>
    /// </remarks>
    public class HumanizerCollectionGrammar : ICollectionFormatter
    {
        private readonly string _defaultSeparator;

        public HumanizerCollectionGrammar(string defaultSeparator = "and")
        {
            _defaultSeparator = defaultSeparator;
        }

        public virtual string Humanize<T>(IEnumerable<T> collection)
        {
            return Humanize(collection, o => o?.ToString(), _defaultSeparator);
        }

        public virtual string Humanize<T>(IEnumerable<T> collection, Func<T, string> objectFormatter)
        {
            return Humanize(collection, objectFormatter, _defaultSeparator);
        }

        public string Humanize<T>(IEnumerable<T> collection, Func<T, object> objectFormatter)
        {
            return Humanize(collection, objectFormatter, _defaultSeparator);
        }

        public virtual string Humanize<T>(IEnumerable<T> collection, string separator)
        {
            return Humanize(collection, o => o?.ToString(), separator);
        }

        public virtual string Humanize<T>(IEnumerable<T> collection, Func<T, string> objectFormatter, string separator)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (objectFormatter == null)
            {
                throw new ArgumentNullException(nameof(objectFormatter));
            }

            return HumanizeDisplayStrings(
                collection.Select(objectFormatter),
                separator);
        }

        public string Humanize<T>(IEnumerable<T> collection, Func<T, object> objectFormatter, string separator)
        {
            if (collection == null)
            {
                throw new ArgumentNullException(nameof(collection));
            }

            if (objectFormatter == null)
            {
                throw new ArgumentNullException(nameof(objectFormatter));
            }

            return HumanizeDisplayStrings(
                collection.Select(objectFormatter).Select(o => o?.ToString()),
                separator);
        }

        private string HumanizeDisplayStrings(IEnumerable<string> strings, string separator)
        {
            var itemsArray = strings
                .Select(item => item == null ? string.Empty : item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToArray();

            var count = itemsArray.Length;

            if (count == 0)
            {
                return string.Empty;
            }

            if (count == 1)
            {
                return itemsArray[0];
            }

            var itemsBeforeLast = itemsArray.Take(count - 1);
            var lastItem = itemsArray.Skip(count - 1).First();

            return string.Format(CultureInfo.CurrentCulture, GetConjunctionFormatString(count),
                string.Join(", ", itemsBeforeLast),
                separator,
                lastItem);
        }

        protected virtual string GetConjunctionFormatString(int itemCount) => "{0} {1} {2}";
    }
}
