using Dapper;

namespace DapperBuilder.Extensions;

public static class QueryBuilderNullableUtils
{
    extension<T>(QueryBuilder<T?> builder)
    {
        public QueryBuilder<T?> SingleCommand(string sql, object? param = null) => builder.AddCommand(c => c.QuerySingleOrDefaultAsync<T>, sql, param);
        public QueryBuilder<T?> FirstCommand(string sql, object? param = null) => builder.AddCommand(c => c.QueryFirstOrDefaultAsync<T>, sql, param);
        public QueryBuilder<T?> ScalarCommand(string sql, object? param = null) => builder.AddCommand(c => c.ExecuteScalarAsync<T>, sql, param);
    }
}