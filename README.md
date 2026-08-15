# Introduction
This NuGet Package, **DapperBuilder**, is a light-weight tool that allows you to create CRUD queries or Select queries using the `Dapper` NuGet Package's extension methods. 
Through the `QueryBuilder<T>` class, a query can be built that acts as a single transaction to perform several SQL queries, 
before either being discarded or committed to the actual database. 

For customization purposes, the actual implementation of the execution of these queries is abstracted to the `IAccessor` interface.
This interface requires implementing a `CancellationToken` property and a `RunQuery<T>(QueryBuilder<T> builder)` method.
By abstracting this to an interface, you can easily unit/integration test any query operation by simply creating a mock of your base implementation.

# Examples
In this paragraph, I'll show some base implementations of the `IAccessor` interface, 
the usage of the abstract class `Access` and 
how you can create a query using the `QueryBuilder<T>` class.

## IAccessor Implementation
### Base Implementation
When creating a class that implements `IAccessor`, this will be the template that you'll have to work with:
```cs
public sealed class Accessor : IAccessor
{
    public CancellationToken Token => throw new NotImplementedException();

    public IAsyncEnumerable<T> RunQuery<T>(QueryBuilder<T> queryBuilder)
    {
        throw new NotImplementedException();
    }
}
```
The `CancellationToken` property is simply to allow for cancelling the asynchronous operation, 
and can be implemented by simply making it an auto-implemented property:
```cs
public CancellationToken Token { get; init; }
```
The `RunQuery<T>` method is the actual query executer that will take the entered queries from the `QueryBuilder<T>` object and yield their result asynchronously.
In most cases, you can just make use of the static method provided in the static class `AccessUtils`:
```cs
public async IAsyncEnumerable<T> RunQuery<T>(QueryBuilder<T> queryBuilder)
{
    await foreach (var value in AccessUtils.RunQueries(queryBuilder, con, trans, Token))
    {
        yield return value;
    }
}
```
For this implementation to work, you must supply a `DbConnection` (refers to `con`) and a `DbTransaction` (refers to `trans`).
This can be done in two ways. In most scenarios, this is the default way to supply these Database objects:
```cs
public async IAsyncEnumerable<T> RunQuery<T>(QueryBuilder<T> queryBuilder)
{
    using var con = await AccessUtils.Connect(ConString, str => new SqlConnection(str), Token);
    using var trans = await con.BeginTransactionAsync();
    await foreach (var value in AccessUtils.RunQueries(queryBuilder, con, trans, Token))
    {
        yield return value;
    }
    await trans.CommitAsync();
}
```
In this example, the `Connect` method will open a connection provided a constructor and a connection string asynchronously.
I'm using the `SqlConnection` object here, but anything implementing `DbConnection` can be used in its place.

By using the `using` expression, the connection and transaction will be disposed of at the end of the method, 
ensuring that no unmanaged resources stay up after the method is done executing.

The `await trans.CommitAsync()` ensures that any changes to the database are committed, 
since any logic class' methods will most likely want the changes to be saved.

### Mock Implementation
To create a mock of this `Accessor` object, 
all we need to do is provide the `DbConnection` and `DbTransaction` explicitly through the constructor using Dependency Injection:
```cs
public sealed class AccessorMock(DbConnection con, DbTransaction trans) : IAccessor
{
    private readonly DbConnection _con = con;
    private readonly DbTransaction _trans = trans;

    public CancellationToken Token { get; init; }

    public async IAsyncEnumerable<T> RunQuery<T>(QueryBuilder<T> queryBuilder)
    {
        await foreach (var value in AccessUtils.RunQueries(queryBuilder, _con, _trans, Token))
        {
            yield return value;
        }
    }
}
```
In this example, I provided any `DbConnection` and `DbTransaction` through the primary constructor, 
allowing these to be saved within the object as the fields `_con` and `_trans`.
And since these have been provided ahead of time, you only need to yield all the query results using the helper method `RunQueries`.

Now, you can Unit Test this by providing your own `DbConnection` and `DbTransaction` 
without having to worry about transactions updating the real database!

## Inheriting from `Access`
To handle an entity from your database, you can inherit from the abstract class `Access`:
```cs
public sealed class PersonAccess(IAccessor accessor) : Access(accessor, SQL, ref _isCreated)
{
    private const string SQL = """
    CREATE TABLE IF NOT EXISTS Person (
        id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
        name TEXT NOT NULL,
        birthDate DATE NOT NULL
    )
    """;
    private static bool _isCreated;
}
```
In this `PersonAccess` class, I used a primary constructor to inject an `IAccessor` object to propagate to the `Access` abstract class.

The second parameter provided is the Table creation SQL, which will be executed from within the `Access` class' constructor.

The third parameter is a reference to a static boolean. 

This boolean indicates whether this specific table has already been created during the run of the program.
> [!WARNING]
> You still need to add a check for if the table exists, since the database lives past the runtime of the program.

Now, let's add an `Insert` method to our `PersonAccess` class:
```cs
internal async Task<int> Insert(Person p) => new QueryBuilder<int>(Accessor)
.NonQueryCommand("INSERT INTO Person VALUES (@ID, @Name, @BirthDate)", p)
.Execute();
```
This method takes in a `Person` DTO record and creates a new instance of the `QueryBuilder<int>` class, 
with a non-query command to insert this object into the database. For more on how these queries work, see the next section.

## Using the `QueryBuilder<T>` Class
### Creation
Now that you know how to create your own `Access` classes, it's time to actually make use of the `QueryBuilder<T>` class.
As shown in the previous section, you can create a `QueryBuilder<T>` object by injecting an `IAccessor` object into it:
```cs
Accessor accessor = new Accessor();
QueryBuilder<int> builder = new QueryBuilder<int>(accessor);
```
### Commands
Now that you've created an instance of this `QueryBuilder<T>` class, you can start adding commands to your query.

#### `AddCommand` Method
Starting off with the most generic method, you can use the `AddCommand` method to add a new command to your query:
```cs
new QueryBuilder<int>(accessor)
.AddCommand(c => c.ExecuteAsync, PersonSQL.Insert, new Person(1, "Name", DateTime.Today));
```
In this example, I first specified a `DapperQuery` delegate, which takes in any `DbConnection`, returning a delegate that takes in a `CommandDefinition` object.

This means any delegate matching the style `connection => commandDefinition => connection.Method(commandDefinition)` can be used here.

The second parameter is the SQL you want to execute. 

> [!NOTE]
> By normal convention, I would recommend putting this in its own dedicated file to satisfy the Single Responsibility Principle (SRP).

The third parameter is the parameter you want to provide as a placeholder for the query, similar to how that works with Dapper's object parameters.

Another way to provide the parameter argument to this method is by specifying a function that takes in the previously executed command:
```cs
new QueryBuilder<int>(accessor)
.AddCommand(c => c.ExecuteAsync, PersonSQL.Insert, new Person(1, "Name", DateTime.Today))
.AddCommand(c => c.ExecuteAsync, PersonSQL.Delete, id => new { ID = id });
```

#### `NonQueryCommand` Extension Method
This extension method will apply the `ExecuteAsync` method automatically:
```cs
new QueryBuilder<int>(accessor)
.NonQueryCommand(PersonSQL.Insert, new Person(1, "Name", DateTime.Today));
```

#### `ScalarCommand` Extension Method
This extension method will apply the `ExecuteScalarAsync<T>` method automatically:
```cs
new QueryBuilder<int>(accessor)
.ScalarCommand(PersonSQL.Count);
```

#### `SingleCommand` Extension Method
This extension method will apply the `SingleOrDefaultAsync<T>` method automatically:
```cs
new QueryBuilder<Person?>(accessor)
.SingleCommand(PersonSQL.ByName, new { Name = "Name" });
```

#### `FirstCommand` Extension Method
This extension method will apply the `FirstOrDefaultAsync<T>` method automatically:
```cs
new QueryBuilder<Person?>(accessor)
.FirstCommand(PersonSQL.ByName, new { Name = "Name" });
```

#### `QueryCommand` Extension Method
This extension method will apply the `QueryAsync<T>` method automatically:
```cs
new QueryBuilder<IEnumerable<Person>>(accessor)
.QueryCommand(PersonSQL.GetTeenagers);
```

#### `QueryCommand<U, ...>` Extension Methods
These extension method overloads will apply each individual `QueryAsync<T, ...>` method automatically, applied with some mapper function:
```cs
new QueryBuilder<IEnumerable<Person>>(accessor)
.QueryCommand<Pet>((person, pet) => { person.Pet = pet; return person; }, PersonSQL.GetAustralians);
```

### `Execute` Methods
Lastly, you can execute these commands using the `Execute` method:
```cs
Person? p = new QueryBuilder<Person?>(accessor)
.SingleCommand(PersonSQL.ByName, new { Name = "Name" })
.Execute(e => e.SingleAsync);
```
This example executes the given `SingleCommand` and yields its value, which is then queried by the provided `SingleAsync` method.
> [!NOTE]
> Any delegate matching a function that takes in an `IAsyncEnumerable<T>` and 
> returns a function that takes in a `CancellationToken`
> is allowed to be used in this method.
> 
> Example: `enumerable => token => enumerable.SingleAsync(token);`

If you don't want to have to specify the `SingleAsync` method every time (since most queries only have one command), you can use the empty overload:
```cs
Person? p = new QueryBuilder<Person?>(accessor)
.SingleCommand(PersonSQL.ByName, new { Name = "Name" })
.Execute();
```
