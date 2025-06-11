using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace ODSaleOrder.API.Services.Dapper
{
    public class DapperRepositories : IDapperRepositories
    {
        private NpgsqlConnection _connection;
        private readonly string _cnn;
        private readonly ILogger<DapperRepositories> _logger;
        public DapperRepositories(ILogger<DapperRepositories> logger)
        {
            _cnn = Environment.GetEnvironmentVariable("CONNECTION");
            _logger = logger;
        }

        public IDbConnection OpenConnection()
        {
            try
            {
                _connection = new NpgsqlConnection(_cnn);
                _connection.Open();
                return _connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }
        public IDbConnection OpenConnectionAsync()
        {
            try
            {
                _connection = new NpgsqlConnection(_cnn);
                _connection.OpenAsync();
                return _connection;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return null;
            }
        }
        public object Query<T>(string query)
        {
            object Result;
            try
            {
                if (_connection == null || _connection.State != ConnectionState.Open)
                    OpenConnection();
                Result = _connection.Query<T>(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                Result = null;
            }
            finally
            {
                _connection.Close();
                _connection.Dispose();
            }
            return Result;
        }
        public async Task<object> QueryFirstOrDefaultAsync<T>(string query)
        {
            object Result;
            try
            {
                if (_connection == null || _connection.State != ConnectionState.Open)
                    OpenConnection();
                Result = await _connection.QueryFirstOrDefaultAsync<T>(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                Result = null;
            }
            finally
            {
                _connection.Close();
                _connection.Dispose();
            }
            return Result;
        }
        public object QueryWithParams<T>(string query, DynamicParameters parameters)
        {
            object Result;
            try
            {
                if (_connection == null || _connection.State != ConnectionState.Open)
                    OpenConnection();
                Result = _connection.Query<T>(query, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                Result = null;
            }
            finally
            {
                _connection.Close();
                _connection.Dispose();
            }
            return Result;
        }
        public bool Execute(string query)
        {
            int Result;
            try
            {
                if (_connection == null || _connection.State != ConnectionState.Open)
                    OpenConnection();
                Result = _connection.Execute(query);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                Result = 0;
            }
            finally
            {
                _connection.Close();
                _connection.Dispose();
            }
            return Result > 0;
        }
        public async Task<int> ExecuteAsync(string query, DynamicParameters parameters)
        {
            int Result;
            try
            {
                if (_connection == null || _connection.State != ConnectionState.Open)
                    OpenConnectionAsync();
                Result = await _connection.ExecuteAsync(query, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                Result = 0;
            }
            finally
            {
                _connection.Close();
                _connection.Dispose();
            }
            return Result;
        }
        public async Task<int> ExecuteAsync(string query, List<DynamicParameters> parameters)
        {
            int Result;
            try
            {
                if (_connection == null || _connection.State != ConnectionState.Open)
                    OpenConnectionAsync();
                Result = await _connection.ExecuteAsync(query, parameters);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                Result = 0;
            }
            finally
            {
                _connection.Close();
                _connection.Dispose();
            }
            return Result;
        }
        public async Task<bool> InsertAsync<T>(T data, string tableName)
        {
            bool status = false;
            try
            {
                // Get the properties of the object
                var properties = typeof(T).GetProperties();

                // Construct column names and parameter names
                var columnNames = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
                var paramNames = string.Join(", ", properties.Select(p => "@" + p.Name));

                // Prepare the parameters for the query
                var parameters = new DynamicParameters();
                foreach (var property in properties)
                {
                    parameters.Add("@" + property.Name, property.GetValue(data));
                }

                // Wrap the table name in quotes for PostgreSQL compatibility
                var tableNameWithSchema = $"\"{tableName}\"";

                // Ensure the connection is open
                if (_connection == null || _connection.State != ConnectionState.Open)
                    OpenConnection();

                // Begin transaction for atomicity (optional, but recommended)
                using (var transaction = _connection.BeginTransaction())
                {
                    try
                    {
                        // Execute the insert query
                        int res = await _connection.ExecuteAsync(
                            $"INSERT INTO {tableNameWithSchema} ({columnNames}) VALUES ({paramNames})",
                            parameters,
                            transaction);

                        // If the result is greater than 0, the insert was successful
                        if (res > 0)
                            status = true;

                        // Commit the transaction
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        // Rollback in case of error
                        transaction.Rollback();
                        _logger.LogError(ex, "Error occurred while inserting data.");
                        status = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while handling the insert operation.");
            }
            finally
            {
                // Close the connection if it's open
                if (_connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Dispose();
                }
            }
            return status;
        }
        public async Task<bool> InsertAsync<T>(IEnumerable<T> data, string tableName)
        {
            bool status = false;
            try
            {
                if (data == null || !data.Any())
                    throw new ArgumentException("The dataList cannot be null or empty.");

                var properties = typeof(T).GetProperties();
                var columnNames = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
                var tableNameWithSchema = $"\"{tableName}\"";

                if (_connection == null || _connection.State != ConnectionState.Open)
                    OpenConnection();

                // Build the parameterized SQL string for insertion
                var paramNames = string.Join(", ", properties.Select(p => "@" + p.Name));
                var insertQuery = $"INSERT INTO {tableNameWithSchema} ({columnNames}) VALUES ({paramNames})";

                using (var transaction = _connection.BeginTransaction())
                {
                    foreach (var element in data)
                    {
                        var parameters = new DynamicParameters();
                        foreach (var property in properties)
                        {
                            parameters.Add("@" + property.Name, property.GetValue(data));
                        }
                        // Execute the insertion for each data object
                        int result = await _connection.ExecuteAsync(insertQuery, parameters, transaction);
                        if (result > 0)
                            status = true;
                        else
                            throw new InvalidOperationException($"Failed to insert data into {tableNameWithSchema}");
                    }
                    // Commit the transaction after all inserts are successful
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while inserting data.");
            }
            finally
            {
                if (_connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Dispose();
                }
            }

            return status;
        }
        public void BulkInsert<T>(List<T> data, string tableName)
        {
            if (data == null || data.Count == 0)
                throw new ArgumentException("The data list cannot be null or empty.");

            try
            {
                // Ensure the connection is open
                if (_connection == null || _connection.State != ConnectionState.Open)
                    OpenConnection();

                // Create a Binary Import writer for PostgreSQL's COPY command
                using (var writer = _connection.BeginBinaryImport($"COPY {tableName} FROM STDIN (FORMAT BINARY)"))
                {
                    var properties = typeof(T).GetProperties();

                    // Start each row with the corresponding properties serialized in binary format
                    foreach (var element in data)
                    {
                        writer.StartRow();

                        foreach (var property in properties)
                        {
                            var value = property.GetValue(element);

                            // Handle null values by skipping them (you can change this behavior based on your needs)
                            if (value == null)
                            {
                                writer.WriteNull();
                            }
                            else
                            {
                                // Assuming all properties are convertible to byte[] or a primitive type that PostgreSQL understands
                                writer.Write(value);
                            }
                        }
                    }
                    // Complete the bulk import
                    writer.Complete();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk insert.");
                throw; // Rethrow the exception after logging it
            }
            finally
            {
                // Close the connection if it's still open
                if (_connection.State == ConnectionState.Open)
                {
                    _connection.Close();
                    _connection.Dispose();
                }
            }
        }
    }
}
