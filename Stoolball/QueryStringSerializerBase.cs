using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;

namespace Stoolball
{
    public abstract class QueryStringSerializerBase
    {
        protected NameValueCollection Serializer { get; private set; } = new NameValueCollection();

        protected void ResetSerializer()
        {
            Serializer = new NameValueCollection();
        }

        protected void Serialize(string? value, string key)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Serializer.Add(key, value);
            }
        }

        protected void Serialize(string? value, string? key, string? defaultValue)
        {
            if (value != defaultValue)
            {
                Serializer.Add(key, value);
            }
        }

        protected void Serialize(int value, string key)
        {
            Serializer.Add(key, value.ToString(CultureInfo.InvariantCulture));
        }

        protected void Serialize(int value, string key, int defaultValue)
        {
            if (value != defaultValue) { Serializer.Add(key, value.ToString(CultureInfo.InvariantCulture)); }
        }

        protected void Serialize(int? value, string key)
        {
            if (value.HasValue) { Serializer.Add(key, value.Value.ToString(CultureInfo.InvariantCulture)); }
        }
        protected void Serialize(int? value, string key, int? defaultValue)
        {
            if (value != defaultValue)
            {
                if (value.HasValue)
                {
                    Serializer.Add(key, value.Value.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    Serializer.Add(key, string.Empty);
                }
            }
        }

        protected void Serialize(DateTimeOffset? value, string key)
        {
            if (value.HasValue) { Serializer.Add(key, value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)); }
        }

        protected void Serialize(DateTimeOffset? value, string key, DateTimeOffset? defaultValue)
        {
            if (value != defaultValue)
            {
                if (value.HasValue)
                {
                    Serialize(value, key);
                }
                else
                {
                    Serializer.Add(key, string.Empty);
                }
            }
        }

        protected void Serialize(bool value, string key)
        {
            if (value) { Serializer.Add(key, value ? "1" : "0"); }
        }

        protected void Serialize(bool value, string key, bool defaultValue)
        {
            if (value != defaultValue) { Serializer.Add(key, value ? "1" : "0"); }
        }

        protected void Serialize(bool? value, string key)
        {
            if (value.HasValue) { Serializer.Add(key, value.Value ? "1" : "0"); }
        }

        protected void Serialize(bool? value, string key, bool? defaultValue)
        {
            if (value != defaultValue)
            {
                if (value.HasValue)
                {
                    Serializer.Add(key, value.Value ? "1" : "0");
                }
                else
                {
                    Serializer.Add(key, string.Empty);
                }
            }
        }

        protected void Serialize(Guid? value, string key)
        {
            if (value.HasValue) { Serializer.Add(key, value.Value.ToString()); }
        }

        protected void Serialize(Guid? value, string key, Guid? defaultValue)
        {
            if (value != defaultValue)
            {
                if (value.HasValue)
                {
                    Serializer.Add(key, value.Value.ToString());
                }
                else
                {
                    Serializer.Add(key, string.Empty);
                }
            }
        }

        protected void Serialize<T>(List<T> value, string key)
        {
            var sortedList = value.Select(x => x?.ToString()).OrderBy(x => x);
            foreach (var item in sortedList)
            {
                Serializer.Add(key, item);
            }
        }

        protected void Serialize<T>(List<T> value, string key, List<T> defaultValues)
        {
            var sortedList = value.Select(x => x?.ToString()).Where(x => !defaultValues.Select(d => d?.ToString()).Contains(x)).OrderBy(x => x);
            foreach (var item in sortedList)
            {
                Serializer.Add(key, item);
            }
        }
    }
}