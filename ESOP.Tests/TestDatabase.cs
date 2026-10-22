using ESOP.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ESOP.Tests;

//Gives each test its own fresh, private SQLite database that only lives in memory.
//Real SQLite (not EF's InMemory provider) is used so AUTOINCREMENT, NOCASE and the unique index behave exactly like in esop.db
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<EsopDbContext> _options;

    public TestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open(); //An in-memory database only exists while its connection stays open
        _options = new DbContextOptionsBuilder<EsopDbContext>().UseSqlite(_connection).Options;

        using var db = CreateContext();
        db.Database.Migrate(); //Builds the tables from the real migrations, so the migrations get tested too
    }

    //A new DbContext per call, like a new web request in the real app
    public EsopDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
