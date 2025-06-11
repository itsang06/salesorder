using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.OrderStatusHistoryService
{
    public interface IOrderStatusHistoryService
    {
        Task<BaseResultModel> SaveStatusHistory(OsorderStatusHistory input, bool isSave = true, bool isCancelFromWeb = false);
        Task<ODMappingOrderStatus> HandleOSMappingStatus(string SoStatus, bool isFromOs = false);
    }
}
