using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.OS;
using RDOS.INVAPI.Infratructure;
using Sys.Common.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Services.SaleOrder.Interface
{
    public interface ISalesOrderService
    {
        Task<BaseResultModel> InsertOrder(SaleOrderModel model, string token, string username, bool includeConfirm = false);
        Task<BaseResultModel> InsertOrderFromFFA(SaleOrderModel model, string token, string username, string generatedNumber, List<Vat> vats, List<INV_InventoryTransaction> listInvTransaction, bool salesPriceIncludeVaT);
        Task<ResultModelWithObject<ListSOModel>> SearchSO(SaleOrderSearchParamsModel parameters, string token = null, bool dispose = false);
        Task<ResultModelWithObject<ListSOModel>> SearchSOv2(SaleOrderSearchParamsModel parameters, string token = null, bool dispose = false, bool isInternal = false);
        Task<ResultModelWithObject<SaleOrderModel>> GetDetailSO(SaleOrderDetailQueryModel query);
        Task<BaseResultModel> DeleteSO(SaleOrderDetailQueryModel query, string username);
        Task<BaseResultModel> UpdateSO(SaleOrderModel model, string token, string username, bool includeConfirm = false);
        Task<BaseResultModel> ConfirmSO(SaleOrderModel model, string token, string username);
        Task<IEnumerable<SaleOrderBaseModel>> CommonGetAllQueryable(SaleOrderSearchParamsModel parameters);
        Task<BaseResultModel> SaveWithConfirm(SaleOrderModel model, string token, string username);
        Task<BaseResultModel> CreateFTC(SO_FirstTimeCustomer model, string username);
        Task<ResultModelWithObject<ListFTCModel>> SearchFTC(FTCEcoparams parameters);
        Task<BaseResultModel> PrintDeliveryNote(List<string> refNumbers, string token, string username);
        Task<List<SO_OrderItems>> CommonGetOrderDetailsRefNumber(List<string> refnumbers);
        Task<BaseResultModel> AdsReport(SaleOrderAdsParams parameters, string token);
        Task<BaseResultModel> CompleteSO(SaleOrderDetailQueryModel query, string token, string username);
        Task<List<SO_OrderItems>> CommonGetItemsByOrderRefNumbers(List<string> OrderRefNumbers);
        Task<BaseResultModel> CancelMutipleDeliveredSO(List<SO_OrderList> OrderList, string distributorCode, string username, string token);
        Task<BaseResultModel> CancelNewSO(SaleOrderModel model, string token, string username, bool isFromOs=false);
        Task<ResultModelWithObject<SearchListModel<SO_OrderItems>>> GetSuggestionItems(SaleOrderSearchParamsModel parameters, string token, bool buyed = true);
        Task<BaseResultModel> UpdateDeliveryResult(List<SaleOrderModel> models, string status, string username, string token);
        Task<ResultModelWithObject<SaleOrderModel>> CommonHandleInternalSoAttribute(SaleOrderModel model, string token);
        Task HandleCancelBudgetFFA(FfasoOrderItem item, FfasoOrderInformation order, string token);
        Task HandleCancelBudgetSO(SO_OrderItems item, SO_OrderInformations order, string token);

        #region Report
        Task<BaseResultModel> SalesSynthesisReport(SummaryRevenueReportQuery parameters, string userName, string token);
        Task<BaseResultModel> ReportTrackingOrder(ReportTrackingOrderQueryModel parameters, string userName, string token);
        Task<BaseResultModel> ReportShippingStatus(ReportOrderShippingQueryModel parameters, string userName, string token);

        Task<BaseResultModel> GetListWareHouseByCustomerId(SaleOrderSearchParamsModel parameters);
        Task<BaseResultModel> SalesDetailReport(SalesReportQuery parameters, string userName, string token);
        Task<BaseResultModel> ProductivityReport(ProductivityReportRequest parameters, string token);
        Task<BaseResultModel> ProductivityByDayReport(ProductivityReportRequest parameters, string token);
        Task<BaseResultModel> ProductivityBySalesReport(ProductivityBySalesReportRequest parameters, string userName, string token);
        Task<BaseResultModel> ProductivityByProductReport(PBPReportRequest parameters, string userName, string token);
        #endregion

        Task<BaseResultModel> GetSaleRepIdByDistributorCode(string DistributorCode);
        Task<BaseResultModel> ProcessPendingDataTrans(OrderPendingTransModel model, string username, string token);
        BaseResultModel SaleOrderDetail(string ExternalOrdNBR, string OrderType);
        Task<BaseResultModel> InsertOrderFromOneShop(SaleOrderModel model, List<INV_InventoryTransaction> listInvTransaction, string token, List<ODMappingOrderStatus> listMappingOrderStatus);
        Task<BaseResultModel> GetPeriodID(DateTime? OrderDate, string token);

        #region External API
        Task<BaseResultModel> ExUpdateOrderSetting(int deliveryLeadDate, string username);
        Task<ResultModelWithObject<ListPromotionDetailReportOrderListModel>> ExTpGetDataForReportDetail(ExPromotionReportEcoParameters request);
        Task<ResultModelWithObject<ListPromotionDetailReportRouteZoneListModel>> ExTpGetRouteZonesOrderForPromotionReport(ExPromotionReportEcoParameters request);
        Task<ResultModelWithObject<ListPromotionDetailReportPointSaleListModel>> ExTpGetCustomersOrderForPromotionReport(ExPromotionReportEcoParameters request);
        Task<ResultModelWithObject<ListPromotionDetailReportOrderListModel>> ExTpGetOrdersForPopupPromotionReport(ExPromotionReportEcoParameters request);
        Task<ResultModelWithObject<ListPromotionDetailReportRouteZoneListModel>> ExTpGetRouteZonesOrderForPopupPromotionReport(ExPromotionReportEcoParameters request);
        Task<ResultModelWithObject<ListPromotionDetailReportPointSaleListModel>> ExTpGetCustomersOrderForPopupPromotionReport(ExPromotionReportEcoParameters request);
        #endregion

        #region Khoa enhance
        Task<BaseResultModel> UpdateDeliveryResultv2(List<string> OrderRefNumberList, string Status, string ReasonCode, string DistributorCode, string UserName, string Token);
        #endregion
    }
}
