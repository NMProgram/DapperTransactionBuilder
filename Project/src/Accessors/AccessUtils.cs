using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using Dapper;

namespace DapperBuilder.Accessors;

public static class AccessUtils
{
    public static async Task<TConn> Connect<TConn>(string conString, Func<string, TConn> connector, CancellationToken token)
    where TConn : DbConnection
    {
        var con = connector(conString);
        await con.OpenAsync(token);
        return con;
    }
    public static async IAsyncEnumerable<T> RunQueries<T>(QueryBuilder<T> builder, IDbConnection con, IDbTransaction trans, [EnumeratorCancellation] CancellationToken token)
    {
        T? prev = default;
        foreach (var (query, sql, param) in builder.Commands)
        {
            yield return prev = await query(con)(new CommandDefinition(sql, param(prev), trans, cancellationToken: token));
        }
    }
}