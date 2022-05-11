using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Stoolball.Metadata;
using Stoolball.Navigation;
using Stoolball.Web.Security;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Stoolball.Web.Models
{
    /// <summary>
    /// A base view model for stoolball data view models. View models must implement <see cref="IPublishedContent"/>,
    /// and inheriting from this class does that for them automatically.
    /// </summary>
    /// <remarks>
    /// Extending the partial class <see cref="StoolballRouter"/> would result in a lot of properties set to <c>null</c>
    /// and not relevant to each view. Inheriting from <see cref="StoolballRouter"/> causes an error. Using this base class
    /// to compose <see cref="StoolballRouter"/> and the actual stoolball data model into a view model works best.
    /// </remarks>
    public abstract partial class BaseViewModel : IPublishedContent, IHasViewMetadata
    {
        private readonly IPublishedContent? _contentModel;
        private readonly IUserService? _userService;

        public BaseViewModel(IPublishedContent? contentModel = null, IUserService? userService = null)
        {
            _contentModel = contentModel;
            _userService = userService;
        }

        private T ThrowMissingContentException<T>() { throw new NotSupportedException($"This property is only available when an {nameof(IPublishedContent)} is passed to the view model constructor."); }

        /// <summary>
        /// Gets the metadata for a view
        /// </summary>
        public ViewMetadata Metadata { get; } = new ViewMetadata();

        /// <summary>
        /// Gets the photo that appears in the header of the site
        /// </summary>
        public IPublishedContent HeaderPhotoWithInheritance() => _contentModel.Value<IPublishedContent>("headerPhoto", fallback: Fallback.ToAncestors) as IPublishedContent;

        public AuthorizedMembersViewModel Authorization { get; set; } = new();

        /// <summary>
        /// Gets the custom stylesheet that should be applied to the page (minus the .css extension)
        /// </summary>
        public string Stylesheet { get { return _contentModel.Value<string>("stylesheet"); } }

        /// <inheritdoc/>
        public List<Breadcrumb> Breadcrumbs { get; } = new List<Breadcrumb>(new[] {
                    new Breadcrumb { Name = Constants.Pages.Home, Url = new Uri(Constants.Pages.HomeUrl, UriKind.Relative) },
                    new Breadcrumb { Name = Constants.Pages.Players, Url = new Uri(Constants.Pages.PlayersUrl, UriKind.Relative) }
        });

        #region Implement IPublishedContent
        public int Id => _contentModel?.Id ?? ThrowMissingContentException<int>();

        public string Name => _contentModel?.Name ?? string.Empty;

        public string UrlSegment => _contentModel?.UrlSegment ?? string.Empty;

        public int SortOrder => _contentModel?.SortOrder ?? ThrowMissingContentException<int>();

        public int Level => _contentModel?.Level ?? ThrowMissingContentException<int>();

        public string Path => _contentModel?.Path ?? string.Empty;

        public int? TemplateId => _contentModel != null ? _contentModel.TemplateId : ThrowMissingContentException<int?>();

        public int CreatorId => _contentModel?.CreatorId ?? ThrowMissingContentException<int>();

        public string CreatorName => _contentModel != null && _userService != null ? _contentModel.CreatorName(_userService) : string.Empty;

        public DateTime CreateDate => _contentModel?.CreateDate ?? ThrowMissingContentException<DateTime>();

        public int WriterId => _contentModel?.WriterId ?? ThrowMissingContentException<int>();

        public string WriterName => _contentModel != null && _userService != null ? _contentModel.WriterName(_userService) : string.Empty;

        public DateTime UpdateDate => _contentModel?.UpdateDate ?? ThrowMissingContentException<DateTime>();

        public string Url => _contentModel?.Url() ?? string.Empty;

        public IReadOnlyDictionary<string, PublishedCultureInfo> Cultures => _contentModel?.Cultures ?? new ReadOnlyDictionary<string, PublishedCultureInfo>(new Dictionary<string, PublishedCultureInfo>());

        public PublishedItemType ItemType => _contentModel?.ItemType ?? PublishedItemType.Unknown;

        public IPublishedContent Parent => _contentModel != null ? _contentModel.Parent : ThrowMissingContentException<IPublishedContent>();

        public IEnumerable<IPublishedContent> Children => _contentModel?.Children ?? Array.Empty<IPublishedContent>();

        public IEnumerable<IPublishedContent> ChildrenForAllCultures => _contentModel?.ChildrenForAllCultures ?? Array.Empty<IPublishedContent>();

        public IPublishedContentType ContentType => _contentModel != null ? _contentModel.ContentType : ThrowMissingContentException<IPublishedContentType>();

        public Guid Key => _contentModel != null ? _contentModel.Key : ThrowMissingContentException<Guid>();

        public IEnumerable<IPublishedProperty> Properties => _contentModel?.Properties ?? Array.Empty<IPublishedProperty>();

        public IPublishedProperty GetProperty(string alias)
        {
            return _contentModel != null ? _contentModel.GetProperty(alias) : ThrowMissingContentException<IPublishedProperty>();
        }

        public bool IsDraft(string? culture = null)
        {
            return _contentModel?.IsDraft(culture) ?? false;
        }

        public bool IsPublished(string? culture = null)
        {
            return _contentModel?.IsPublished(culture) ?? true;
        }

        #endregion // Implement IPublishedContent
    }
}