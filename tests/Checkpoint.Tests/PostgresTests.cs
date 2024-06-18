using Checkpoint.Tests.Fixtures.Postgres;
using Xunit.Abstractions;

namespace Checkpoint.Tests;

/// <summary>
/// Tests for postgres adapter
/// </summary>
public class PostgresTests : BasePostgresIntegrationTest
{
	/// <inheritdoc />
	public PostgresTests(ITestOutputHelper output, PostgresSqlContainerFixture fixture) : base(output, fixture)
	{
	}

	[Fact(DisplayName = "Restore should restore all tables to checkpoint state")]
	public async Task ShouldRestoreDatabaseToCheckpoint()
	{
		// Arrange
		// Prepare table for current test
		await ExecuteNonQueryAsync("CREATE TABLE foo (bar int)");

		await ExecuteNonQueryAsync("INSERT INTO foo VALUES (1)");

		// Act
		// One record in the table before creating the checkpoint
		var checkpoint = await Checkpoint.CreateAsync(DbContext);

		await ExecuteNonQueryAsync("INSERT INTO foo VALUES (2)");

		// Before calling the table restore there were two record in the table
		// After the restore we should have only one record
		await checkpoint.RestoreAsync();

		// Assert
		var count = await ExecuteScalarAsync<long>("SELECT COUNT (*) FROM foo");

		Assert.Equal(1, count);

		// Drop arrange tables for current test
		await ExecuteNonQueryAsync("DROP TABLE foo");
	}

	[Fact(DisplayName = "Cleanup should delete all the rows in all tables")]
	public async Task ShouldCleanupDatabaseToZeroState()
	{
		// Arrange
		// Prepare table for current test
		await ExecuteNonQueryAsync("CREATE TABLE foo (bar int)");

		await ExecuteNonQueryAsync("INSERT INTO foo VALUES (1)");

		// Act
		// One record in the table before creating the checkpoint
		var checkpoint = await Checkpoint.CreateAsync(DbContext);

		await ExecuteNonQueryAsync("INSERT INTO foo VALUES (2)");

		// Before calling the table cleanup there were two record in the table
		// After the cleanup we should have zero record because all rows were truncated
		await checkpoint.CleanupAsync();

		var count = await ExecuteScalarAsync<long>("SELECT COUNT (*) FROM foo");

		Assert.Equal(0, count);

		// Drop arrange tables for current test
		await ExecuteNonQueryAsync("DROP TABLE foo");
	}
}