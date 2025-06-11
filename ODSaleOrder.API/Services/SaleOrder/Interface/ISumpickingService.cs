using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using Sys.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.SaleOrder.Interface
{
    public interface ISumpickingService
    {
        Task<ResultModelWithObject<SumpickingDetailModel>> GetDetailSumpicking(SumpickingDetailQueryModel query, string token, bool includeItems = true);
        Task<BaseResultModel> Insert(SumpickingModel model, string token, string username, bool includeConfirm = false);
        Task<BaseResultModel> Update(SumpickingModel model, string token, string username, bool includeConfirm = false);
        Task<BaseResultModel> Confirm(SumpickingModel model, string token, string username);
        Task<BaseResultModel> SearchSumpicking(SumpickingSearchModel parameters);
        Task<BaseResultModel> GetSumpickingItems(List<string> listOrderRefNumber, string token);
        Task<BaseResultModel> SaveWithConfirm(SumpickingModel model, string token, string username);
    }
}
