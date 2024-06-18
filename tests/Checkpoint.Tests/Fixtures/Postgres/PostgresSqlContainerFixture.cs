using Npgsql;
using Testcontainers.PostgreSql;

namespace Checkpoint.Tests.Fixtures.Postgres;

/// <summary>
/// An instance of a docker container with PostgreSQL.
/// </summary>
public class PostgresSqlContainerFixture : DatabaseFixture<PostgreSqlContainer, NpgsqlConnection>
{
	/// <inheritdoc />
	public PostgresSqlContainerFixture()
	{
		var database = $"postgres-integration-{Guid.NewGuid():N}";

		Container = new PostgreSqlBuilder()
			.WithName(database)
			.WithCleanUp(true)
			.Build();
	}

	/// <inheritdoc />
	public override async Task InitializeAsync() => await Container
		.StartAsync();

	/// <inheritdoc />
	public override async Task DisposeAsync() => await Container
		.DisposeAsync();

	/// <inheritdoc />
	public override void Dispose() => GC.SuppressFinalize(this);
}