using System.Data;
using Microsoft.Data.Sqlite;

namespace FactorySimulation.Tests.Utilities;

/// <summary>
/// Base test fixture providing database connection management.
/// Uses a shared in-memory SQLite connection for the test class.
/// Each test class gets a fresh database.
/// </summary>
public class TestFixture : IAsyncLifetime
{
    private SqliteConnection? _connection;

    /// <summary>
    /// Gets the database connection for tests.
    /// The connection stays open for the lifetime of the fixture.
    /// </summary>
    public SqliteConnection Connection => _connection
        ?? throw new InvalidOperationException("Connection not initialized. Call InitializeAsync first.");

    /// <summary>
    /// Creates a connection factory function that returns the shared connection.
    /// Use this when constructing repositories for testing.
    /// </summary>
    public Func<IDbConnection> ConnectionFactory => () => Connection;

    /// <summary>
    /// Initializes the test fixture with database schema and seed data
    /// </summary>
    public async Task InitializeAsync()
    {
        _connection = TestDbFactory.CreateOpenInMemoryConnection();
        await TestDbFactory.CreateSchemaAsync(_connection);
        await TestDbFactory.SeedBasicDataAsync(_connection);
    }

    /// <summary>
    /// Cleans up the test fixture by closing the connection
    /// </summary>
    public Task DisposeAsync()
    {
        _connection?.Close();
        _connection?.Dispose();
        return Task.CompletedTask;
    }
}
