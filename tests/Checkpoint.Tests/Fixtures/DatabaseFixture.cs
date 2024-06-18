using DotNet.Testcontainers.Containers;

namespace Checkpoint.Tests.Fixtures;

/// <summary>
/// The fixture of the TestContainers database class.
/// </summary>
/// <typeparam name="TDockerContainer"> The type <see cref="IContainer" />. </typeparam>
/// <typeparam name="TDatabaseConnection"> The type of connection to the database. </typeparam>
public abstract class DatabaseFixture<TDockerContainer, TDatabaseConnection> : IAsyncLifetime, IDisposable
	where TDockerContainer : IContainer
{
	/// <summary>
	/// Получает или задает TestContainers.
	/// </summary>
	public TDockerContainer Container { get; protected init; }

	/// <inheritdoc />
	public abstract Task InitializeAsync();

	/// <inheritdoc />
	public abstract Task DisposeAsync();

	/// <inheritdoc />
	public abstract void Dispose();
}