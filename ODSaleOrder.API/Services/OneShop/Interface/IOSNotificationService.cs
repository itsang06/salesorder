using ODSaleOrder.API.Models.OS;
using Sys.Common.Models;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.OneShop.Interface
{
    public interface IOSNotificationService
    {
        Task<BaseResultModel> SendNotification(OSNotificationModel req, string token, bool isFromOs = false);
    }
}
