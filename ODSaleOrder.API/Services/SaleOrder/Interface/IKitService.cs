using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using Sys.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.SaleOrder.Interface
{
    public interface IKitService
    {
        Task<BaseResultModel> CalculateOrderHeaderByKitCode(SO_OrderItems item);
    }
}
