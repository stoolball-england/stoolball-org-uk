using System.Collections.Specialized;
using System.Linq;
using System.Net;

namespace Stoolball
{
    public static class NameValueCollectionExtensions
    {
        public static string ToQueryString(this NameValueCollection nvc)
        {
            return "?" + string.Join("&", nvc.AllKeys.SelectMany(key => nvc.GetValues(key).Select(value => $"{WebUtility.UrlEncode(key)}={WebUtility.UrlEncode(value)}")));
        }
    }
}
