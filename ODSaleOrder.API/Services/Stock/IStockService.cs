using ODSaleOrder.API.Models;
using Sys.Common.Models;

namespace ODSaleOrder.API.Services.Stock
{
    public interface IStockService
    {
        BaseResultModel GetProductWithStock(ProductWithStockModel model, string token);

        BaseResultModel GetOrderReasonList();
    }
}
