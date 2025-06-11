using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.SalesOrder;
using Sys.Common.Models;
using System.Collections.Generic;

namespace ODSaleOrder.API.Services.CaculateTax
{
    public interface ICalculateTaxService
    {
        void CaculateOrderLineTax(OrderLineTaxModel orderLineTaxModel, out double ord_Line_TaxAfter_Amt, out double ord_Line_TaxBefore_Amt);
        void CaculateShippingTax(ShippedLineTax shippedLineTax, out double shipped_Line_TaxAfter_Amt, out double shipped_Line_TaxBefore_Amt, out decimal shipped_Line_Extend_Amt);
        double? CalculateCommissionDiscount(SO_OrderInformations orderInformation, SO_OrderItems orderItem);
        SO_OrderItems CalculateTaxInCludeVAT(ref SO_OrderItems order);
        SO_OrderItems CalculateTaxNotInCludeVAT(ref SO_OrderItems order);
        SO_OrderInformations CalculateTotalVAT(ref SaleOrderModel order);
        bool GetSalesPriceIncludeVaT();
        BaseResultModel InventoryBulkCreate(SO_OrderInformations orderInfo, List<SO_OrderItems> orderItems, string oldStatus, string requestStatus, string token);
    }
}