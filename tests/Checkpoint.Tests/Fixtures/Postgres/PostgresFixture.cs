using System.Data;
using System.Data.Common;
using System.Text;
using Npgsql;
using Xunit.Abstractions;

namespace Checkpoint.Tests.Fixtures.Postgres;

/// <summary>
/// Base class for PostgresSQL integration tests.
/// </summary>
public abstract class BasePostgresIntegrationTest : IAsyncLifetime, IClassFixture<PostgresSqlContainerFixture>
{
	/// <summary>
	/// PostgreSQL error and notice details are included on
	/// PostgresException.Detail and PostgresNotice.Detail.
	/// These can contain sensitive data.
	/// </summary>
	private const string IncludeErrorDetail = ";Include Error Detail=True";

	/// <summary>
	/// That indicates if security-sensitive information, such as the password, is not
	/// returned as part of the
	/// connection if the connection is open or has ever been in an open state.
	/// </summary>
	private const string PersistSecurityInfo = ";PersistSecurityInfo=True";

	/// <summary>
	/// PostgreSQL container fixture.
	/// </summary>
	private readonly PostgresSqlContainerFixture _fixture;

	/// <summary>
	/// An active connection for database access.
	/// </summary>
	protected NpgsqlConnection DbContext { get; private set; }

	protected BasePostgresIntegrationTest(ITestOutputHelper output, PostgresSqlContainerFixture fixture) => _fixture = fixture;

	/// <inheritdoc />
	public async Task InitializeAsync()
	{
		var connectionString = new StringBuilder(_fixture.Container.GetConnectionString())
			.Append(IncludeErrorDetail)
			.Append(PersistSecurityInfo)
			.ToString();

		var connection = new NpgsqlConnection(connectionString);

		if (connection.State is ConnectionState.Closed)
		{
			await connection.OpenAsync();
		}

		DbContext = connection;
	}

	/// <inheritdoc />
	public async Task DisposeAsync()
	{
		if (DbContext.State is ConnectionState.Open)
		{
			await DbContext.CloseAsync();
		}

		await DbContext.DisposeAsync();
	}

	/// <inheritdoc cref="DbCommand.ExecuteNonQueryAsync()" />
	protected virtual async Task ExecuteNonQueryAsync(string query)
	{
		await using var command = new NpgsqlCommand(query, DbContext);
		await command.ExecuteNonQueryAsync();
	}

	/// <inheritdoc cref="DbCommand.ExecuteScalarAsync()" />
	protected virtual async Task<T?> ExecuteScalarAsync<T>(string query)
	{
		await using var command = new NpgsqlCommand(query, DbContext);

		return (T?) await command.ExecuteScalarAsync();
	}
}