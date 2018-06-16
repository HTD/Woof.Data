using System.Data.Common;
using System.Threading.Tasks;

namespace Woof.DataEx {

    /// <summary>
    /// Interface for simple asynchronous database operations.
    /// </summary>
    public interface IDbSource {

        /// <summary>
        /// Creates new SQL input parameter.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        /// <returns><see cref="DbParameter"/>.</returns>
        DbParameter I(string name, object value);

        /// <summary>
        /// Creates new SQL input / output parameter.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Parameter value.</param>
        /// <returns><see cref="DbParameter"/>.</returns>
        DbParameter IO(string name, object value);

        /// <summary>
        /// Creates new SQL output parameter.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <returns><see cref="DbParameter"/>.</returns>
        DbParameter O(string name);

        /// <summary>
        /// Executes stored procedure asynchronously.
        /// </summary>
        /// <param name="procedure">Stored procedure name.</param>
        /// <param name="parameters">Parameters to pass.</param>
        /// <returns>Optional asynchronous result code of the operation (used with some databases).</returns>
        Task<int> ExecuteAsync(string procedure, params DbParameter[] parameters);

        /// <summary>
        /// Gets multiple datasets asynchronously.
        /// </summary>
        /// <param name="procedure">Stored procedure name.</param>
        /// <param name="parameters">Parameters to pass.</param>
        /// <returns>Multiple datasets.</returns>
        Task<object[][][]> GetDataAsync(string procedure, params DbParameter[] parameters);

        /// <summary>
        /// Gets a single record asynchronously.
        /// </summary>
        /// <typeparam name="T">Record type.</typeparam>
        /// <param name="procedure">Stored procedure name.</param>
        /// <param name="parameters">Parameters to pass.</param>
        /// <returns>A single record of type <typeparamref name="T"/></returns>
        Task<T> GetRecordAsync<T>(string procedure, params DbParameter[] parameters) where T : new();

        /// <summary>
        /// Gets a single scalar value of type <typeparamref name="T"/> asynchronously.
        /// </summary>
        /// <typeparam name="T">Scalar type.</typeparam>
        /// <param name="procedure">Stored procedure name.</param>
        /// <param name="parameters">Parameters to pass.</param>
        /// <returns>A single scalar value of type <typeparamref name="T"/>.</returns>
        Task<T> GetScalarAsync<T>(string procedure, params DbParameter[] parameters);

        /// <summary>
        /// Gets a table of <see cref="object"/>[] records asynchronously.
        /// </summary>
        /// <param name="procedure">Stored procedure name.</param>
        /// <param name="parameters">Parameters to pass.</param>
        /// <returns>A table of <see cref="object"/>[] records.</returns>
        Task<object[][]> GetTableAsync(string procedure, params DbParameter[] parameters);

        /// <summary>
        /// Gets a table of <typeparamref name="T"/> records asynchronously.
        /// </summary>
        /// <typeparam name="T">Record type.</typeparam>
        /// <param name="procedure">Stored procedure name.</param>
        /// <param name="parameters">Parameters to pass.</param>
        /// <returns>A table of <typeparamref name="T"/> records.</returns>
        Task<T[]> GetTableAsync<T>(string procedure, params DbParameter[] parameters) where T : new();

    }

}