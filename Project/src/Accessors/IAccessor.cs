namespace DapperBuilder.Accessors;

public interface IAccessor
{
    CancellationToken Token { get; }
    IAsyncEnumerable<T> RunQuery<T>(QueryBuilder<T> queryBuilder);
}