using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MySalesTracker.Infrastructure.Persistence;

namespace MySalesTracker.Tests;

/// <summary>
/// Disposable SQL Server database used by tests that must exercise real migrations and indexes.
/// Returns <see langword="null"/> when no test server is reachable and none was configured,
/// so the caller can skip. A configured <c>MYSALESTRACKER_TEST_SQL</c> makes every failure fatal.
/// </summary>
internal sealed class SqlServerTestDatabase : IAsyncDisposable
{
    const string DefaultConnectionString =
        "Server=127.0.0.1,1435;User ID=sa;Password=yourStrong(!)Password;Encrypt=false;TrustServerCertificate=true;";

    SqlServerTestDatabase(string connectionString, string databaseName)
    {
        ConnectionString = connectionString;
        DatabaseName = databaseName;
    }

    public string ConnectionString { get; }

    public string DatabaseName { get; }

    public static async Task<SqlServerTestDatabase?> TryCreateAsync(CancellationToken ct = default)
    {
        var configured = Environment.GetEnvironmentVariable("MYSALESTRACKER_TEST_SQL");
        var explicitlyConfigured = !string.IsNullOrWhiteSpace(configured);
        var template = explicitlyConfigured ? configured! : DefaultConnectionString;
        var databaseName = $"MySalesTracker_Test_{Guid.NewGuid():N}";

        var master = new SqlConnectionStringBuilder(template) { InitialCatalog = "master" };

        if (explicitlyConfigured)
        {
            // Explicit configuration must fail loudly; a silent skip would hide a broken SQL setup.
            await CreateDatabaseAsync(master.ConnectionString, databaseName, ct);
        }
        else
        {
            if (!await CanConnectAsync(master.ConnectionString, ct))
            {
                return null;
            }

            await CreateDatabaseAsync(master.ConnectionString, databaseName, ct);
        }

        var target = new SqlConnectionStringBuilder(template) { InitialCatalog = databaseName };
        return new SqlServerTestDatabase(target.ConnectionString, databaseName);
    }

    static async Task<bool> CanConnectAsync(string connectionString, CancellationToken ct)
    {
        await using var connection = new SqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
    }

    static async Task CreateDatabaseAsync(string masterConnectionString, string databaseName, CancellationToken ct)
    {
        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE [{databaseName}]";
        await command.ExecuteNonQueryAsync(ct);
    }

    public AppDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task ExecuteAsync(string sql, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<object?> ScalarAsync(string sql, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return await command.ExecuteScalarAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        var master = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(master.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"IF DB_ID('{DatabaseName}') IS NOT NULL BEGIN ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DatabaseName}]; END";
        await command.ExecuteNonQueryAsync();
    }
}
