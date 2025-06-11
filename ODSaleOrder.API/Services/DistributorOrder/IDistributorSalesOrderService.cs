using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models.Customer;
using ODSaleOrder.API.Models.DistributorSalesOrder;
using Sys.Common.Models;
using System.Collections.Generic;

namespace ODSaleOrder.API.Services.DistributorOrder
{
    public interface IDistributorSalesOrderService
    {
        BaseResultModel CancelDistributorOrder(DistributorCancelOrderModel request, string token);
        BaseResultModel CreateDistributorOrder(DistributorOrderModel request, string DistributorCode);
        BaseResultModel GenerateOrderRefNumber(string DistributorCode);
        BaseResultModel GetCustomerByDistributorPaging(SearchCustomerModel input, string DistributorCode);
        BaseResultModel GetCustomerShiptoDetail(string CustomerCode, string DistributorCode);
        BaseResultModel GetOrderDetail(string OrderRefNumber);
        BaseResultModel GetOrderSetting();
        BaseResultModel GetReturnOrder(string OrderRefNumber);
        BaseResultModel HandleCancelBookedBudget(List<AppliedPromotionModel> AppliedPromotionList, DistributorCancelOrderModel request, string token);
        BaseResultModel HandleCancelBookedInventory(List<SO_OrderItems> orderItems, DistributorCancelOrderModel request, string token);
        BaseResultModel UpdateStatus(UpdateDistributorOrderModel request, string token);
    }
}