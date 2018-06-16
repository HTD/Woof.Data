using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Woof.DataEx {

    /// <summary>
    /// Base class for simple SQL data models.
    /// </summary>
    public abstract class DbModel : IDisposable {

        /// <summary>
        /// Gets the model's connection.
        /// </summary>
        protected DbConnection Connection { get; }

        /// <summary>
        /// Gets the model's connection string.
        /// </summary>
        protected string ConnectionString { get; }

        /// <summary>
        /// Gets or sets the SQL command timeout.
        /// </summary>
        protected int Timeout { get; set; } = 300;

        /// <summary>
        /// Creates the model with a connection string.
        /// Microsoft SQL database connection is used.
        /// </summary>
        /// <param name="connectionString"></param>
        public DbModel(string connectionString) {
            ConnectionString = connectionString;
            Connection = new SqlConnection(connectionString);
        }

        /// <summary>
        /// Creates the model with a connection.
        /// </summary>
        /// <param name="connection"></param>
        public DbModel(DbConnection connection) {
            Connection = connection;
            ConnectionString = Connection.ConnectionString;
        }

        /// <summary>
        /// Starts a database transaction.
        /// </summary>
        public void BeginTransaction() => Transaction = Connection.BeginTransaction();

        /// <summary>
        /// Commits the database transaction.
        /// </summary>
        public void CommitTransaction() {
            Transaction.Commit();
            Transaction.Dispose();
            Transaction = null;
        }

        /// <summary>
        /// Rolls back a transaction from a pending state.
        /// </summary>
        public void RollbackTransaction() {
            Transaction.Rollback();
            Transaction.Dispose();
            Transaction = null;
        }

        /// <summary>
        /// Executes a stored procedure with some optional parameters.
        /// Returns whatever database engine returns for non-query mode.
        /// </summary>
        /// <param name="procedure">Stored procedure name.</param>
        /// <param name="parameters">Parameters in SQL digestable form. Use <see cref="DataTable"/> for table types.</param>
        /// <returns>Number of affected rows or other integer the specific database engine returns for non-query mode.</returns>
        protected int Exec(string procedure, params object[] parameters) {
            if (Connection.State != ConnectionState.Open) Connection.Open();
            using (var cmd = Connection.CreateCommand()) {
                if (Transaction != null) cmd.Transaction = Transaction;
                cmd.CommandTimeout = Timeout;
                cmd.CommandText = procedure;
                cmd.CommandType = CommandType.StoredProcedure;
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Executes a stored procedure with some optional parameters.
        /// Returns whatever database engine returns for non-query mode.
        /// </summary>
        /// <param name="procedure">Stored procedure name.</param>
        /// <param name="parameters">Parameters in SQL digestable form. Use <see cref="DataTable"/> for table types.</param>
        /// <returns>Number of affected rows or other integer the specific database engine returns for non-query mode.</returns>
        protected async Task<int> ExecAsync(string procedure, params object[] parameters) {
            if (Connection.State != ConnectionState.Open) await Connection.OpenAsync();
            using (var cmd = Connection.CreateCommand()) {
                if (Transaction != null) cmd.Transaction = Transaction;
                cmd.CommandTimeout = Timeout;
                cmd.CommandText = procedure;
                cmd.CommandType = CommandType.StoredProcedure;
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// Executes a stored procedure with some optional parameters to fetch data.
        /// </summary>
        /// <typeparam name="T">Returned record type.</typeparam>
        /// <param name="procedure">Stored procedure name.</param>
        /// <param name="parameters">Parameters in SQL digestable form. Use <see cref="DataTable"/> for table types.</param>
        /// <returns>A collection of records.</returns>
        protected IEnumerable<T> QueryAndGet<T>(string procedure, params object[] parameters) where T : new() {
            if (Connection.State != ConnectionState.Open) Connection.Open();
            using (var cmd = Connection.CreateCommand()) {
                if (Transaction != null) cmd.Transaction = Transaction;
                cmd.CommandTimeout = Timeout;
                cmd.CommandText = procedure;
                cmd.CommandType = CommandType.StoredProcedure;
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read()) yield return reader.ReadToPropertiesOfNew<T>();
            }
        }

        /// <summary>
        /// Executes a stored procedure with some optional parameters to fetch data.
        /// </summary>
        /// <typeparam name="T">Returned record type.</typeparam>
        /// <param name="procedure">Stored procedure name.</param>
        /// <param name="parameters">Parameters in SQL digestable form. Use <see cref="DataTable"/> for table types.</param>
        /// <returns>A collection of records.</returns>
        protected async Task<T[]> QueryAndGetAsync<T>(string procedure, params object[] parameters) where T : new() {
            if (Connection.State != ConnectionState.Open) await Connection.OpenAsync();
            var data = new List<T>();
            using (var cmd = Connection.CreateCommand()) {
                if (Transaction != null) cmd.Transaction = Transaction;
                cmd.CommandTimeout = Timeout;
                cmd.CommandText = procedure;
                cmd.CommandType = CommandType.StoredProcedure;
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                using (var reader = cmd.ExecuteReader())
                    while (await reader.ReadAsync()) data.Add(reader.ReadToPropertiesOfNew<T>());
            }
            return data.ToArray();
        }

        /// <summary>
        /// Executes a stored procedure with some optional parameters to fetch data.
        /// </summary>
        /// <param name="procedure">Stored procedure name.</param>
        /// <param name="parameters">Parameters in SQL digestable form. Use <see cref="DataTable"/> for table types.</param>
        /// <returns>A collection of rows.</returns>
        protected IEnumerable<object[]> QueryAndGet(string procedure, params object[] parameters) {
            if (Connection.State != ConnectionState.Open) Connection.Open();
            using (var cmd = Connection.CreateCommand()) {
                if (Transaction != null) cmd.Transaction = Transaction;
                cmd.CommandTimeout = Timeout;
                cmd.CommandText = procedure;
                cmd.CommandType = CommandType.StoredProcedure;
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read()) {
                        var values = new object[reader.FieldCount];
                        reader.GetValues(values);
                        yield return values;
                    }
            }
        }

        /// <summary>
        /// Executes a stored procedure with some optional parameters to fetch data.
        /// </summary>
        /// <param name="procedure">Stored procedure name.</param>
        /// <param name="parameters">Parameters in SQL digestable form. Use <see cref="DataTable"/> for table types.</param>
        /// <returns>A collection of rows.</returns>
        protected async Task<object[][]> QueryAndGetAsync(string procedure, params object[] parameters) {
            if (Connection.State != ConnectionState.Open) await Connection.OpenAsync();
            var data = new List<object[]>();
            using (var cmd = Connection.CreateCommand()) {
                if (Transaction != null) cmd.Transaction = Transaction;
                cmd.CommandTimeout = Timeout;
                cmd.CommandText = procedure;
                cmd.CommandType = CommandType.StoredProcedure;
                if (parameters != null) cmd.Parameters.AddRange(parameters);
                using (var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync()) {
                        var values = new object[reader.FieldCount];
                        reader.GetValues(values);
                        data.Add(values);
                    }
            }
            return data.ToArray();
        }

        /// <summary>
        /// Disposes the connection (and transaction if applicable).
        /// </summary>
        public void Dispose() {
            Transaction?.Dispose();
            Connection.Dispose();
        }

        private DbTransaction Transaction;

    }

}