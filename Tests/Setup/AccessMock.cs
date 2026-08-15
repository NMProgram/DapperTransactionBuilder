namespace TestSetup;

using System.Data.Common;
using DapperBuilder;
using DapperBuilder.Accessors;
using Microsoft.Data.Sqlite;

public class AccessMock(SqliteConnection con, DbTransaction trans) : IAccessor
{
    private readonly SqliteConnection _con = con;
    private readonly DbTransaction _trans = trans;
    public CancellationToken Token { get; init; }

    public async IAsyncEnumerable<T> RunQuery<T>(QueryBuilder<T> queryBuilder)
    {
        await foreach (var value in AccessUtils.RunQueries(queryBuilder, _con, _trans, Token)) yield return value;
    }
}
