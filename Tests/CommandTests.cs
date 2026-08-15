namespace BuilderTests;

using Dapper;
using DapperBuilder;
using DapperBuilder.Accessors;
using DapperBuilder.Extensions;
using Microsoft.Data.Sqlite;
using TestSetup;

public class CommandTests
{
    static readonly SqliteConnection _con;
    static readonly Person _testResult = new Person(1, "Name", 30);
    const string SQLInsert = "INSERT INTO Person VALUES (@ID, @Name, @Age); SELECT LAST_INSERT_ROWID()";
    const string SQLFind = "SELECT * FROM Person WHERE name = @Name";
    static CommandTests()
    {
        _con = AccessUtils.Connect("Data Source=:memory:", v => new SqliteConnection(v), default).GetAwaiter().GetResult();
        _con.Execute("""
        CREATE TABLE IF NOT EXISTS Person (
            id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            age INTEGER NOT NULL
        )
        """);
    }
    [Fact]
    public async Task AddCommand_AddsAnySQLCommand()
    {
        // Arrange
        using var trans = await _con.BeginTransactionAsync(TestContext.Current.CancellationToken);
        AccessMock mock = new AccessMock(_con, trans);
        // Act
        int value = await new QueryBuilder<int>(mock)
        .AddCommand(c => c.ExecuteAsync, SQLInsert, _testResult)
        .Execute();

        Person? p = await new QueryBuilder<Person?>(mock)
        .AddCommand(c => async cmd => await (await c.QueryMultipleAsync(cmd)).ReadFirstAsync<Person>(), SQLFind, new { Name = "Name" })
        .Execute();
        // Assert
        Assert.Equal(1, value); 
        Assert.Equal(_testResult, p);
    }

    [Fact]
    public async Task NonQueryCommand_ExecutesNonQuery()
    {
        // Arrange
        using var trans = await _con.BeginTransactionAsync(TestContext.Current.CancellationToken);
        AccessMock mock = new AccessMock(_con, trans);
        // Act
        int value = await new QueryBuilder<int>(mock)
        .NonQueryCommand(SQLInsert, _testResult)
        .Execute();

        Person? p = await new QueryBuilder<Person?>(mock)
        .AddCommand(c => c.QuerySingleOrDefaultAsync<Person>, SQLFind, new { Name = "Name" })
        .Execute();
        // Assert
        Assert.Equal(1, value); 
        Assert.Equal(_testResult, p);
    }

    [Fact]
    public async Task SingleCommand_ExecutesSingleOrDefault()
    {
        // Arrange
        using var trans = await _con.BeginTransactionAsync(TestContext.Current.CancellationToken);
        AccessMock mock = new AccessMock(_con, trans);
        // Act
        int value = await new QueryBuilder<int>(mock)
        .NonQueryCommand(SQLInsert, _testResult)
        .Execute();

        Person? p = await new QueryBuilder<Person?>(mock)
        .SingleCommand(SQLFind, new { Name = "Name" })
        .Execute();
        // Assert
        Assert.Equal(1, value); 
        Assert.Equal(_testResult, p);
    }

    [Fact]
    public async Task FirstCommand_ExecutesFirstOrDefault()
    {
        // Arrange
        using var trans = await _con.BeginTransactionAsync(TestContext.Current.CancellationToken);
        AccessMock mock = new AccessMock(_con, trans);
        // Act
        int value = await new QueryBuilder<int>(mock)
        .NonQueryCommand(SQLInsert, _testResult)
        .Execute();

        Person? p = await new QueryBuilder<Person?>(mock)
        .FirstCommand(SQLFind, new { Name = "Name" })
        .Execute();
        // Assert
        Assert.Equal(1, value); 
        Assert.Equal(_testResult, p);
    }

    [Fact]
    public async Task ScalarCommand_ExecutesScalar()
    {
        // Arrange
        using var trans = await _con.BeginTransactionAsync(TestContext.Current.CancellationToken);
        AccessMock mock = new AccessMock(_con, trans);
        // Act
        int value = await new QueryBuilder<int>(mock)
        .ScalarCommand(SQLInsert, _testResult)
        .ScalarCommand(SQLInsert, _testResult with { ID = 2 })
        .ScalarCommand(SQLInsert, _testResult with { ID = 9 })
        .ScalarCommand(SQLInsert, _testResult with { ID = 22 })
        .Execute(c => c.LastAsync);

        Person? p = await new QueryBuilder<Person?>(mock)
        .AddCommand(c => c.QueryFirstOrDefaultAsync<Person>, SQLFind, new { Name = "Name" })
        .Execute();
        // Assert
        Assert.Equal(22, value);
        Assert.Equal(_testResult, p);
    }

    [Fact]
    public async Task QueryCommand_ExecutesQueryAsync()
    {
        // Arrange
        using var trans = await _con.BeginTransactionAsync(TestContext.Current.CancellationToken);
        AccessMock mock = new AccessMock(_con, trans);
        // Act
        int value = await new QueryBuilder<int>(mock)
        .NonQueryCommand(SQLInsert, _testResult)
        .NonQueryCommand(SQLInsert, _testResult with { ID = 2 })
        .Execute(c => c.SumAsync);

        IEnumerable<Person> people = await new QueryBuilder<IEnumerable<Person>>(mock)
        .QueryCommand(SQLFind, new { Name = "Name" })
        .Execute();
        // Assert
        Assert.Equal(2, value); 
        Assert.Contains(_testResult, people);
        Assert.Contains(_testResult with { ID = 2 }, people);
    }
}
