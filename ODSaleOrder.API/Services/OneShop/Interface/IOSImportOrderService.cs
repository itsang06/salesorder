using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.OS;
using Sys.Common.Models;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.OneShop.Interface
{
    public interface IOSImportOrderService
    {
        Task<ResultModelWithObject<ListOsOrderModel>> GetListOrder(SearchOsOrderModel req);
        Task<BaseResultModel> ImportListOrder(ImportListOSOrder dataInput, string token, string username);
        Task<BaseResultModel> CancelListOrder(ImportListOSOrder dataInput, string token, string username, bool isFromOs = true);
    }
}
