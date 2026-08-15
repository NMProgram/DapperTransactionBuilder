namespace AccessTests;
using Microsoft.Data.Sqlite;
using TestSetup;
public class AbstractAccessTests
{
    [Fact]
    public async Task AccessConstructor_CreatesTableOnce_NoExceptionThrown()
    {
        // Arrange
        using var con = new SqliteConnection("Data Source=:memory:");
        await con.OpenAsync(TestContext.Current.CancellationToken);
        using var trans = await con.BeginTransactionAsync(TestContext.Current.CancellationToken);
        // Act
        PetAccess access = new PetAccess(new AccessMock(con, trans));
        PetAccess access2 = new PetAccess(new AccessMock(con, trans));
    }
}