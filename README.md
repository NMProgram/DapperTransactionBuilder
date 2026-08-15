# Introduction
This NuGet Package, **DapperBuilder**, is a light-weight tool that allows you to create CRUD queries using the `Dapper` NuGet Package's extension methods. 
Through the `QueryBuilder<T>` class, a query can be built that acts as a single transaction to perform several SQL queries, 
before either being discarded or committed to the actual database. 

For customization purposes, the actual implementation of the execution of these queries is abstracted to the `IAccessor` interface.
This interface requires implementing a `CancellationToken` property and a `RunQuery<T>(QueryBuilder<T> builder)` method.
By abstracting this to an interface, you can easily unit/integration test any query operation by simply creating a mock of your base implementation.

# Examples
In this paragraph, I'll show some base implementations of the `IAccessor` interface, 
the usage of the abstract class `Access` and 
how you can create a query using the `QueryBuilder<T>` class.

## IAccessor Implementation
When creating a class that implements `IAccessor`, this will be the template that you'll have to work with:
```cs
public sealed class Accessor : IAccessor
{
    public CancellationToken Token => throw new NotImplementedException();

    public IAsyncEnumerable<T> RunQuery<T>(QueryBuilder<T> queryBuilder)
    {
        throw new NotImplementedException();
    }
}
```
The `CancellationToken` property is simply to allow for cancelling the asynchronous operation, 
and can be implemented by simply making it an auto-implemented property:
```cs
public CancellationToken Token { get; init; }
```
The `RunQuery<T>` method is the actual query executer that will take the entered queries from the `QueryBuilder<T>` object and yield their result asynchronously.
In most cases, you can just make use of the static method provided in the static class `AccessUtils`:
```cs
public async IAsyncEnumerable<T> RunQuery<T>(QueryBuilder<T> queryBuilder)
{
    await foreach (var value in AccessUtils.RunQueries(queryBuilder, con, trans, Token))
    {
        yield return value;
    }
}
```
For this implementation to work, you must supply a `DbConnection` (refers to `con`) and a `DbTransaction` (refers to `trans`).
This can be done in two ways. In most scenarios, this is the default way to supply these Database objects:
```cs
public async IAsyncEnumerable<T> RunQuery<T>(QueryBuilder<T> queryBuilder)
{
    using var con = await AccessUtils.Connect(ConString, str => new SqlConnection(str), Token);
    using var trans = await con.BeginTransactionAsync();
    await foreach (var value in AccessUtils.RunQueries(queryBuilder, con, trans, Token))
    {
        yield return value;
    }
    await trans.CommitAsync();
}
```
In this example, the `Connect` method will open a connection provided a constructor and a connection string asynchronously.
I'm using the `SqlConnection` object here, but anything implementing `DbConnection` can be used in its place.

By using the `using` expression, the connection and transaction will be disposed of at the end of the method, 
ensuring that no unmanaged resources stay up after the method is done executing.

The `await trans.CommitAsync()` ensures that any changes to the database are committed, 
since any logic class' methods will most likely want the changes to be saved.
