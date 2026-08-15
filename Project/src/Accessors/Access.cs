using DapperBuilder.Extensions;

namespace DapperBuilder.Accessors;

public abstract class Access
{
    protected IAccessor Accessor { get; }
    protected Access(IAccessor accessor, string sql, ref bool isCreated)
    {
        Accessor = accessor;
        if (Interlocked.CompareExchange(ref isCreated, true, false)) return;
        QueryBuilder<int>.CreateTable(sql, accessor);
    }
}
