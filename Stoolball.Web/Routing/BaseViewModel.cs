using System;
using System.Collections.Generic;
using Umbraco.Core.Models.PublishedContent;

namespace Stoolball.Web.Routing
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
    public abstract class BaseViewModel : IPublishedContent
    {
        private readonly IPublishedContent _contentModel;

        public BaseViewModel(IPublishedContent contentModel)
        {
            _contentModel = contentModel;
        }

        public int Id => _contentModel.Id;

        public string Name => _contentModel.Name;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "Implementing the IPublishedContent interface")]
        public string UrlSegment => _contentModel.UrlSegment;

        public int SortOrder => _contentModel.SortOrder;

        public int Level => _contentModel.Level;

        public string Path => _contentModel.Path;

        public int? TemplateId => _contentModel.TemplateId;

        public int CreatorId => _contentModel.CreatorId;

        public string CreatorName => _contentModel.CreatorName;

        public DateTime CreateDate => _contentModel.CreateDate;

        public int WriterId => _contentModel.WriterId;

        public string WriterName => _contentModel.WriterName;

        public DateTime UpdateDate => _contentModel.UpdateDate;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1056:Uri properties should not be strings", Justification = "Implementing the IPublishedContent interface")]
        public string Url => _contentModel.Url;

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

        public bool IsDraft(string culture = null)
        {
            return _contentModel.IsDraft(culture);
        }

        public bool IsPublished(string culture = null)
        {
            return _contentModel.IsPublished(culture);
        }
    }
}