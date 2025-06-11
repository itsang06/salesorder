using ODSaleOrder.API.Models;
using Sys.Common.Models;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.SaleOrder.Interface
{
    public interface ISaleOrderReturnService
    {
        Task<BaseResultModel> InsertOrder(SaleOrderModel model, string token, string username, bool includeConfirm = false);
        Task<ResultModelWithObject<ListSOModel>> SearchSOReturn(SaleOrderSearchParamsModel parameters);
        Task<BaseResultModel> GetDetailSOReturn(SaleOrderDetailQueryModel query);
        Task<BaseResultModel> DeleteSOReturn(SaleOrderDetailQueryModel query, string username);
        Task<BaseResultModel> UpdateSOReturn(SaleOrderModel model, string token, string username, bool includeConfirm = false);
        Task<BaseResultModel> Confirm(SaleOrderModel model, string token, string username);
        Task<BaseResultModel> SaveWithConfirm(SaleOrderModel model, string token, string username);

    }
}
