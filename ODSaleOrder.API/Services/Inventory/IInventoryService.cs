using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using RDOS.INVAPI.Infratructure;
using Sys.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.Inventory
{
    public interface IInventoryService
    {
        Task<ResultModelWithObject<INV_AllocationDetail>> GetAllocationDetailCurrent(QueryAllocationModel req);
        Task<ResultModelWithObject<INV_AllocationDetail>> GetListAllocationDetailCurrent(QueryAllocationModel req);
        Task<BaseResultModel> UpdateBooked(INV_AllocationDetail allocatioonDetail, BookAllocationModel req, List<INV_InventoryTransaction> listInvTransaction);
        Task<BaseResultModel> CancelBooked(INV_AllocationDetail allocatioonDetail, BookAllocationModel req, List<INV_InventoryTransaction> listInvTransaction);
        Task<ResultModelWithObject<List<PrincipalWarehouseLocation>>> GetListPrincipalWarehouseLocation();
        Task<List<INV_InventoryTransaction>> GetTransactionsByOneShopID(string oneShopID);
        Task<List<INV_InventoryTransaction>> GetTransactionsByFfaVisitId(string ffaVisitId, string orderType);
        Task<BaseResultModel> CancelBookedFFAOrder(INV_InventoryTransaction input, string username);
    }
}
