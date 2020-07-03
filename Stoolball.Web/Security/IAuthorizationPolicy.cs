using Umbraco.Web.Security;

namespace Stoolball.Web.Security
{
    public interface IAuthorizationPolicy<T>
    {
        /// <summary>
        /// Gets whether the current identity is authorized to delete the given item 
        /// </summary>
        /// <returns></returns>
        /// <remarks>It's recommended to inject MembershipHelper but GetCurrentMember() returns null (https://github.com/umbraco/Umbraco-CMS/blob/2f10051ee9780cd22d4d1313e5e7c6b0bc4661b1/src/Umbraco.Web/UmbracoHelper.cs#L98)</remarks>
        bool CanDelete(T item, MembershipHelper membershipHelper);
    }
}