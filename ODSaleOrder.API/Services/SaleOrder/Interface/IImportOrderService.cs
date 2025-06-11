using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using RDOS.INVAPI.Infratructure;
using Sys.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.SaleOrder.Interface
{
    public interface IImportOrderService
    {
        Task<ResultModelWithObject<ListFfaModel>> GetListOrderFfa(SearchFfaOrderModel req);
        Task<ResultModelWithObject<FfaOrderGroupModel>> GetDetailFfaOrder(string orderNumber);
        Task<BaseResultModel> ImportAllOrder(string token, string username, SearchFfaOrderModel req);
        Task<BaseResultModel> ImportListOrder(ImportListFfaOrder dataInput, string token, string username);
        Task<BaseResultModel> CancelFFAOrders(ImportListFfaOrder dataInput, string token, string username);
        Task<BaseResultModel> CancelFFAOrdersV2(List<CancelListFfaOrder> dataInput, string token, string username);

        // Common
        Task<BaseResultModel> HandleCalculateBaselineDate();
        Task<ResultModelWithObject<ResultInventoryItemRealTimeModel>> GetInventoryItemRealTime(string token, string wareHouseCode, string distributorCode, string itemCode);
        Task<ResultModelWithObject<List<PrincipalWarehouseLocation>>> GetListPrincipalWarehouseLocation();
        Task<INV_AllocationDetail> GetAllocationDetailCurrent(string itemCode, string locationCode, string distributorCode, string wareHouseCode);
        //Task<BaseResultModel> UpdateBooked(UpdateBookedAllocationModel model);
        Task<BaseResultModel> NotifyMobileOrderImportFailed(string token, SendNotifiMobileModel dataInput);
    }
}
