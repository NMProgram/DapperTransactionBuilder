using Dapper;

namespace DapperBuilder.Extensions;

public static class QueryBuilderEnumerableUtils
{
    extension<T>(QueryBuilder<IEnumerable<T>> builder)
    {
        public QueryBuilder<IEnumerable<T>> QueryCommand(string sql, object? param = null) 
            => builder.AddCommand(c => c.QueryAsync<T>, sql, param);
        public QueryBuilder<IEnumerable<T>> QueryCommand<U>(Func<T, U, T> mapper, string sql, object? param = null) 
            => builder.AddCommand(c => cmd => c.QueryAsync(cmd, mapper), sql, param);
        public QueryBuilder<IEnumerable<T>> QueryCommand<U, V>(Func<T, U, V, T> mapper, string sql, object? param = null) 
            => builder.AddCommand(c => cmd => c.QueryAsync(cmd, mapper), sql, param);
        public QueryBuilder<IEnumerable<T>> QueryCommand<U, V, W>(Func<T, U, V, W, T> mapper, string sql, object? param = null) 
            => builder.AddCommand(c => cmd => c.QueryAsync(cmd, mapper), sql, param);
        public QueryBuilder<IEnumerable<T>> QueryCommand<U, V, W, X>(Func<T, U, V, W, X, T> mapper, string sql, object? param = null) 
            => builder.AddCommand(c => cmd => c.QueryAsync(cmd, mapper), sql, param);
        public QueryBuilder<IEnumerable<T>> QueryCommand<U, V, W, X, Y>(Func<T, U, V, W, X, Y, T> mapper, string sql, object? param = null) 
            => builder.AddCommand(c => cmd => c.QueryAsync(cmd, mapper), sql, param);
        public QueryBuilder<IEnumerable<T>> QueryCommand<U, V, W, X, Y, Z>(Func<T, U, V, W, X, Y, Z, T> mapper, string sql, object? param = null) 
            => builder.AddCommand(c => cmd => c.QueryAsync(cmd, mapper), sql, param);
    }
}