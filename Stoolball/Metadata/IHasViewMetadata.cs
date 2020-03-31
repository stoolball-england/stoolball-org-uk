namespace Stoolball.Metadata
{
    public interface IHasViewMetadata
    {
        /// <summary>
        /// Gets the metadata for a view
        /// </summary>
        ViewMetadata Metadata { get; }
    }
}