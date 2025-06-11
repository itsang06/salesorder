using Dapper;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.Dapper
{
    public interface IDapperRepositories
    {
        IDbConnection OpenConnection();
        IDbConnection OpenConnectionAsync();
        object Query<T>(string query);
        Task<object> QueryFirstOrDefaultAsync<T>(string query);
        object QueryWithParams<T>(string query, DynamicParameters parameters);
        bool Execute(string query);
        Task<int> ExecuteAsync(string query, DynamicParameters parameters);
        Task<int> ExecuteAsync(string query, List<DynamicParameters> parameters);
        Task<bool> InsertAsync<T>(T data, string tableName);
        Task<bool> InsertAsync<T>(IEnumerable<T> data, string tableName);
        void BulkInsert<T>(List<T> data, string tableName);
    }
}
