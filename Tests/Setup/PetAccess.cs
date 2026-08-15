namespace TestSetup;

using DapperBuilder.Accessors;

public class PetAccess(IAccessor accessor) : Access(accessor, SQL, ref _isCreated)
{
    private static bool _isCreated;
    private const string SQL = """
    CREATE TABLE Pet (
        id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL
    )
    """;
}
