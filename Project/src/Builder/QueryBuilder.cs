using System.Collections.Immutable;
using System.Data;
using Dapper;

namespace DapperBuilder;
using DapperBuilder.Accessors;

public sealed class QueryBuilder<T>(IAccessor access)
{
    public record Query(DapperQuery DapperQuery, string SQL, Func<T?, object?> Parameter);
    public delegate Command DapperQuery(IDbConnection con);
    public delegate Task<T> Command(CommandDefinition cmd);
    public delegate Func<CancellationToken, ValueTask<T>> AsyncQuery(IAsyncEnumerable<T> asyncEnumerable);

    private readonly IAccessor _access = access;
    public ImmutableArray<Query> Commands { get; private set; } = [];

    public QueryBuilder<T> AddCommand(DapperQuery query, string sql, Func<T?, object?> param)
    {
        Commands = Commands.Add(new(query, sql, param));
        return this;
    }
    public QueryBuilder<T> AddCommand(DapperQuery query, string sql, object? param = null) 
        => AddCommand(query, sql, _ => param);

    public async ValueTask<T> Execute(AsyncQuery query) => await query(ExecuteQueries())(_access.Token);
    public async ValueTask<T> Execute() => await Execute(c => c.SingleAsync);
    private async IAsyncEnumerable<T> ExecuteQueries()
    {
        await foreach (var value in _access.RunQuery(this)) yield return value;
        Commands = default;
    }
}
