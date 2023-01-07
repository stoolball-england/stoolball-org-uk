namespace Stoolball.Data.Abstractions
{
    public class RepositoryResult<TStatus, TResult>
    {
        public TStatus? Status { get; set; }
        public TResult? Result { get; set; }
    }
}
