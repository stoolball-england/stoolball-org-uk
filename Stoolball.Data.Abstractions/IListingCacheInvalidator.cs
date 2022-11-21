namespace Stoolball.Data.Abstractions
{
    public interface IListingCacheInvalidator<T>
    {
        void InvalidateCache();
    }
}