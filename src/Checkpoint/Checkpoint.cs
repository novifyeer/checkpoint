using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Checkpoint;

/// <summary>
/// Checkpoint provides the ability to rollback data during testing and development
/// to maintain data integrity.
/// </summary>
public class Checkpoint
{
	private readonly DbConnection _dbConnection;

	private Checkpoint(DbConnection dbConnection) => _dbConnection = dbConnection;

	/// <summary>
	/// Creates a checkpoint that captures the current state of all user tables in the
	/// public schema, creating temporary copies of each table. <br />
	/// Checkpoint provides the ability to rollback data during testing and development
	/// to maintain data integrity. <br />
	/// Use appropriate methods to restore data <see cref="RestoreAsync" /> from a
	/// checkpoint or delete all rows from tables <see cref="CleanupAsync" />.
	/// </summary>
	/// <param name="connection">
	/// The database connection where the checkpoint will be
	/// created
	/// </param>
	/// <returns> Returns a <see cref="Checkpoint" /> object. </returns>
	public static async Task<Checkpoint> CreateAsync(DbConnection connection)
	{
		const string query =
			"""
			CREATE TEMP TABLE temp_table_list AS
			SELECT table_name
			FROM information_schema.tables
			WHERE table_schema = 'public'
			  AND table_type = 'BASE TABLE';

			CREATE OR REPLACE PROCEDURE pg_temp.create_checkpoint()
			    LANGUAGE plpgsql AS
			$$
			DECLARE
			    current_table_name TEXT;
			BEGIN
			    FOR current_table_name IN
			        SELECT table_name
			        FROM temp_table_list
			        LOOP
			            EXECUTE format('CREATE TEMP TABLE temp_%s AS SELECT * FROM %I;', current_table_name, current_table_name);
			        END LOOP;
			END
			$$;

			CREATE PROCEDURE pg_temp.restore_to_checkpoint(identity_clause TEXT)
			    LANGUAGE plpgsql AS
			$$
			DECLARE
			    current_table_name TEXT;
			BEGIN
			    FOR current_table_name IN
			        SELECT table_name
			        FROM temp_table_list
			        LOOP
			            EXECUTE format('TRUNCATE %I %s;', current_table_name, identity_clause);
			            EXECUTE format('INSERT INTO %I SELECT * FROM temp_%I;', current_table_name, current_table_name);
			        END LOOP;
			END
			$$;

			CREATE OR REPLACE PROCEDURE pg_temp.cleanup_tables(identity_clause TEXT)
			    LANGUAGE plpgsql AS
			$$
			DECLARE
			    current_table_name TEXT;
			BEGIN
			    FOR current_table_name IN
			        SELECT table_name
			        FROM temp_table_list
			        LOOP
			            EXECUTE format('TRUNCATE %I %s;', current_table_name, identity_clause);
			        END LOOP;
			END
			$$;

			CALL pg_temp.create_checkpoint();
			""";

		await using var command = connection.CreateCommand();
		command.CommandText = query;

		await command.ExecuteNonQueryAsync();

		return new(connection);
	}

	/// <summary>
	/// Restores the database to the checkpoint. <br />
	/// The restoration process truncates existing tables and inserts data from
	/// temporary tables created during checkpoint creation. <br />
	/// This method facilitates reverting database state during testing or development
	/// by ensuring consistent data recovery.
	/// </summary>
	public async Task RestoreAsync()
	{
		const string query = "pg_temp.restore_to_checkpoint";

		await using var command = _dbConnection.CreateCommand();
		command.CommandText = query;
		command.CommandType = CommandType.StoredProcedure;

		var identityParameter = command.CreateParameter();
		identityParameter.ParameterName = "identity_clause";
		identityParameter.Value = "RESTART IDENTITY";
		identityParameter.Direction = ParameterDirection.Input;

		command.Parameters.Add(identityParameter);

		await command.ExecuteNonQueryAsync();
	}

	/// <summary>
	/// Cleans up tables in the database. <br />
	/// The cleanup process involves truncating each table specified in the list of
	/// tables.
	/// This method facilitates deleting all data from tables during testing or
	/// development to ensure data integrity.
	/// </summary>
	public async Task CleanupAsync()
	{
		const string query = "pg_temp.cleanup_tables";

		await using var command = _dbConnection.CreateCommand();
		command.CommandText = query;
		command.CommandType = CommandType.StoredProcedure;

		var identityParameter = command.CreateParameter();
		identityParameter.ParameterName = "identity_clause";
		identityParameter.Value = "RESTART IDENTITY";
		identityParameter.Direction = ParameterDirection.Input;

		command.Parameters.Add(identityParameter);

		await command.ExecuteNonQueryAsync();
	}
}