using System;
using System.Collections.Generic;
using Stoolball.Metadata;
using Stoolball.Navigation;
using Stoolball.Security;
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
        private readonly IPublishedContent _contentModel;
        private readonly IUserService _userService;

        public BaseViewModel(IPublishedContent contentModel, IUserService userService)
        {
            _contentModel = contentModel;
            _userService = userService;
        }

        /// <summary>
        /// Gets the metadata for a view
        /// </summary>
        public ViewMetadata Metadata { get; } = new ViewMetadata();

        /// <summary>
        /// Gets the photo that appears in the header of the site
        /// </summary>
        public IPublishedContent HeaderPhotoWithInheritance() => _contentModel.Value<IPublishedContent>("headerPhoto", fallback: Fallback.ToAncestors) as IPublishedContent;

        public Dictionary<AuthorizedAction, bool> IsAuthorized { get; internal set; } = new Dictionary<AuthorizedAction, bool>();

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
        public int Id => _contentModel.Id;

        public string Name => _contentModel.Name;

        public string UrlSegment => _contentModel.UrlSegment;

        public int SortOrder => _contentModel.SortOrder;

        public int Level => _contentModel.Level;

        public string Path => _contentModel.Path;

        public int? TemplateId => _contentModel.TemplateId;

        public int CreatorId => _contentModel.CreatorId;

        public string CreatorName => _contentModel.CreatorName(_userService);

        public DateTime CreateDate => _contentModel.CreateDate;

        public int WriterId => _contentModel.WriterId;

        public string WriterName => _contentModel.WriterName(_userService);

        public DateTime UpdateDate => _contentModel.UpdateDate;

        public string Url => _contentModel.Url();

        public IReadOnlyDictionary<string, PublishedCultureInfo> Cultures => _contentModel.Cultures;

        public PublishedItemType ItemType => _contentModel.ItemType;

        public IPublishedContent Parent => _contentModel.Parent;

        public IEnumerable<IPublishedContent> Children => _contentModel.Children;

        public IEnumerable<IPublishedContent> ChildrenForAllCultures => _contentModel.ChildrenForAllCultures;

        public IPublishedContentType ContentType => _contentModel.ContentType;

        public Guid Key => _contentModel.Key;

        public IEnumerable<IPublishedProperty> Properties => _contentModel.Properties;

        public IPublishedProperty GetProperty(string alias)
        {
            return _contentModel.GetProperty(alias);
        }

        public bool IsDraft(string? culture = null)
        {
            return _contentModel.IsDraft(culture);
        }

        public bool IsPublished(string? culture = null)
        {
            return _contentModel.IsPublished(culture);
        }

        #endregion // Implement IPublishedContent
    }
}