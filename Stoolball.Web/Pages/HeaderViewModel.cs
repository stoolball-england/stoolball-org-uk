using System.Collections.Generic;
using Stoolball.Metadata;
using Stoolball.Statistics;
using Stoolball.Web.Models;
using Stoolball.Web.Navigation;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Stoolball.Web.Pages
{
    public class HeaderViewModel : IHasViewMetadata
    {
        private readonly IHasViewMetadata _metadata;
        public HeaderViewModel(IHasViewMetadata metadata)
        {
            _metadata = metadata ?? throw new System.ArgumentNullException(nameof(metadata));
        }

        public Player? Player { get; set; }

        public ViewMetadata Metadata => _metadata.Metadata;

        public string Stylesheet => _metadata.Stylesheet;

        public List<Breadcrumb> Breadcrumbs => _metadata.Breadcrumbs;

        public IPublishedContent HeaderPhotoWithInheritance()
        {
            return _metadata.HeaderPhotoWithInheritance();
        }
    }
}
