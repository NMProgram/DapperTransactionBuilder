using Dapper;
using DapperBuilder.Accessors;

namespace DapperBuilder.Extensions;

public static class QueryBuilderIntUtils
{
    extension(QueryBuilder<int> builder)
    {
        public static ValueTask<int> CreateTable(string sql, IAccessor access) => new QueryBuilder<int>(access)
        .NonQueryCommand(sql)
        .Execute(a => a.SingleAsync);
        public QueryBuilder<int> NonQueryCommand(string sql, object? param = null) => builder.AddCommand(c => c.ExecuteAsync, sql, param);
    }
}
