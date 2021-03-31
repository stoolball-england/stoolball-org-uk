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

        protected void Serialize(string value, string key)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                Serializer.Add(key, value.ToString(CultureInfo.InvariantCulture));
            }
        }

        protected void Serialize(int value, string key)
        {
            Serializer.Add(key, value.ToString(CultureInfo.InvariantCulture));
        }

        protected void Serialize(int? value, string key)
        {
            if (value.HasValue) { Serializer.Add(key, value.Value.ToString(CultureInfo.InvariantCulture)); }
        }

        protected void Serialize(DateTimeOffset? value, string key)
        {
            if (value.HasValue) { Serializer.Add(key, value.Value.ToString("yyyy-M-d", CultureInfo.InvariantCulture)); }
        }

        protected void Serialize(bool value, string key)
        {
            if (value) { Serializer.Add(key, value ? "1" : "0"); }
        }

        protected void Serialize(bool? value, string key)
        {
            if (value.HasValue) { Serializer.Add(key, value.Value ? "1" : "0"); }
        }

        protected void Serialize(Guid? value, string key)
        {
            if (value.HasValue) { Serializer.Add(key, value.Value.ToString()); }
        }

        protected void Serialize<T>(List<T> value, string key)
        {
            var sortedList = value.Select(x => x.ToString()).OrderBy(x => x);
            foreach (var item in sortedList)
            {
                Serializer.Add(key, item);
            }
        }
    }
}