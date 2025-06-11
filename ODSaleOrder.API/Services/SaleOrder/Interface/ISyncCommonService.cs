using ODSaleOrder.API.Models.SyncHistory;
using Sys.Common.Models;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.SaleOrder.Interface
{
    public interface ISyncCommonService
    {
        Task<BaseResultModel> SaveLogSync(StagingSyncDataHistoryModel logNew);
    }
}
