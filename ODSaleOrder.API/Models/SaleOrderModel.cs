using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Models.ReportModel;
using Sys.Common.Models;

namespace ODSaleOrder.API.Models
{
    public class SaleOrderModel : SO_OrderInformations
    {

        public List<SO_OrderItems> OrderItems { get; set; }
        public string SumpickingRefNumber { get; set; }
        public string Shipper { get; set; }
    }

    // public class SaleOrderModel : SO_OrderInformations
    // {
    //     public List<SO_OrderItems> OrderItems { get; set; }
    // }

    public class SaleOrderBaseModel
    {
        public SO_OrderInformations OrderInformation { get; set; }
        public SO_OrderItems OrderItem { get; set; }
    }

    public class SaleOrderGroupedModel
    {
        public SO_OrderInformations OrderInformation { get; set; }
        public List<SO_OrderItems> OrderItems { get; set; }
    }

    public class SaleOrderGroupedModelV2
    {
        public CommonSoOrderModel OrderInformation { get; set; }
        public List<CommonSoOrderModel> OrderItems { get; set; }
    }

    public class SaleOrderDetailQueryModel
    {
        public string OrderRefNumber { get; set; }
        public string DistributorCode { get; set; }
    }

    public class OrderListQueryModel
    {
        public List<string> OrderRefNumber { get; set; }
        public string DistributorCode { get; set; }
        public string Status { get; set; }
    }

    public class SaleOrderSearchParamsModel : EcoparamsWithGenericFilter
    {
        public List<string> OrderRefNumbers { get; set; }
        public string DistributorCode { get; set; }
        public string CustomerCode { get; set; }
        public string WareHouseCode { get; set; }
        public bool IncludeSumpicking { get; set; } = false;
        public string Shipper { get; set; }
        public string SumaryPickingNbr { get; set; }
        public List<string> ListDistributor { get; set; } = new List<string>();
        public string DeliveryProcessSearchValue { get; set; }
        public string PickingListSearchValue { get; set; }
        public List<OutletFilter> OutletFilters { get; set; }
        public string Address { get; set; }
        public List<string> StatusFilter { get; set; } = new List<string>();
        public DateTime? UpdatedDate { get; set; }
        public bool ExcludeBaseLine { get; set; } = false;
        public DateTime? OrderDate { get; set; }
        public bool IncludeItem { get; set; } = false;
    }

    public class OutletFilter
    {
        public string CustomerCode { get; set; }
        public string ShiptoCode { get; set; }
    }


    public class ListSOModel
    {
        public List<SaleOrderModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }



    public class ListFTCModel
    {
        public List<SO_FirstTimeCustomer> Items { get; set; }
        public MetaData MetaData { get; set; }
    }

    public class FTCEcoparams : EcoParameters
    {
        public string DistributorCode { get; set; }
    }


    public class SaleOrderAdsParams
    {
        public string DistributorCode { get; set; }
        public string ItemGroupCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class SO_CancelRequestModel
    {
        public List<SO_OrderList> OrderList { get; set; }
        public string distributorCode { get; set; }
    }

    public class SO_OrderList
    {
        public string OrderRefNumber { get; set; }
        public string ReasonCode { get; set; }

    }

    public class SODetail
    {
        public SO_OrderInformations SOInfomation { get; set; }
        public List<SO_OrderItems> SOOrderItems { get; set; }
    }

    public class FfaOrderModel
    {
        public FfasoOrderInformation FfasoOrderInformation { get; set; }
        public List<FfasoOrderItem> FfasoOrderItem { get; set; }
        public List<FfadsSoLot> FfadsSoLot { get; set; }
        public List<FfadsSoPayment> FfadsSoPayment { get; set; }
    }

    public class SaleOrderDetail
    {
        public SODetail SO { get; set; }
        public FfaOrderModel FFA { get; set; }
    }

    #region Report
    public class FromDateToDateNullableModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public bool IsOver3Month()
        {
            if (FromDate.HasValue && ToDate.HasValue)
            {
                return ToDate.Value > FromDate.Value.AddMonths(3);
            }
            return false;
        }
        public BaseResultModel IsOver3MonthResult()
        {
            return new BaseResultModel
            {
                IsSuccess = false,
                Message = "FromDateAndToDateIsOver3Month",
                Code = 400
            };
        }
    }
    public class FromDateToDateModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool IsOver3Month()
        {
            return ToDate > FromDate.AddMonths(3);
        }
        public BaseResultModel IsOver3MonthResult()
        {
            return new BaseResultModel
            {
                IsSuccess = false,
                Message = "FromDateAndToDateIsOver3Month",
                Code = 400
            };
        }
    }
    public class ReportOrderShippingQueryModel : FromDateToDateNullableModel
    {
        public string DistributorCode { get; set; }
        public string ItemGroupCode { get; set; }
    }


    public class ReportTrackingOrderQueryModel : FromDateToDateNullableModel
    {
        public string DistributorCode { get; set; }
        public string SaleRepId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }


    public class ReportOrderShippingModel
    {
        public string DistributorCode { get; set; }
        public string DistributorDescription { get; set; } //Later
        public List<ReportOrderShippingDetail> OrderItems { get; set; }
    }

    public class ReportOrderShippingDetail
    {
        public string InventoryID { get; set; }
        public string Description { get; set; }
        public int SlthungOrder { get; set; }
        public int SllocOrder { get; set; }
        public int SlchaiOrder { get; set; }
        public int SlthungShipped { get; set; }
        public int SllocShipped { get; set; }
        public int SlchaiShipped { get; set; }
    }

    public class ReportTrackingOrderModel
    {
        public string DistributorCode { get; set; }
        public string DistributorDescription { get; set; } //Later
        public ListReportTrackingOrderResultModel OrderInformations { get; set; }
    }
    public class ReportTrackingOrderResultModel
    {
        public string OrderRefNumber { get; set; }
        public string SalesRepID { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Amount { get; set; } //Tổng doanh số được xác nhận trên đơn ahfng
        public string Status { get; set; }
        public int TotalBaseQty { get; set; }
    }

    public class ListReportTrackingOrderResultModel
    {
        public List<ReportTrackingOrderResultModel> Items { get; set; } = new List<ReportTrackingOrderResultModel>();
        public MetaData MetaData { get; set; }
    }


    public class SalesReportQuery : FromDateToDateNullableModel
    {
        public string DistributorCode { get; set; }
        public string RouteZoneCode { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public bool IsListDetail { get; set; } = false;
    }
    public class SalesReportModel
    {
        public string DistributorCode { get; set; }
        public string DistributorDescription { get; set; } //Later
        public double TotalAmount { get; set; }
        public double VAT { get; set; }
        public double DiscountAmount { get; set; }
        public double Revenue { get; set; }
        //public List<SalesReportDetailModel> OrderInformations { get; set; } = new List<SalesReportDetailModel>();
        public ListSalesReportDetailModel OrderInformations { get; set; } = null;
    }

    public class SalesReportDetailModel
    {
        public string RouteZoneCode { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public string SONumber { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal VAT { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ListSalesReportDetailModel
    {
        public List<SalesReportDetailModel> Items { get; set; } = new List<SalesReportDetailModel>();
        public MetaData MetaData { get; set; }
    }

    public class SummaryRevenueReportQuery : SalesReportQuery
    {
        public string ReportType { get; set; } //Amount //Revenue
    }

    public class RevenueReportRespone
    {
        public string DistributorCode { get; set; }
        public List<RevenueReportRouteZoneRespone> ListRouteZone { get; set; } = new List<RevenueReportRouteZoneRespone>();
    }

    public class RevenueReportRouteZoneRespone
    {
        public string RouteZoneCode { get; set; }
        public List<RevenueReportDetailRespone> ListData { get; set; } = new List<RevenueReportDetailRespone>();
    }

    public class RevenueReportDetailRespone
    {
        public DateTime OrderDate { get; set; }
        public decimal Amount { get; set; }
    }

    public class HODataModel
    {
        public bool IsHO { get; set; }
        public List<string> Distributors { get; set; }
    }

    // RP06
    public class ProductivityReportRequest : FromDateToDateModel
    {
        public string DistributorCode { get; set; }
        public string TerritoryValue { get; set; }
        public string TerritoryStructureCode { get; set; }
        public string SaleOrgCode { get; set; }
        public List<string> ListRouteZoneCode { get; set; }
    }
    public class ProductivityReportRouteZoneRespone
    {
        public string RouteZoneCode { get; set; }
        public List<ProductivityDetailRespone> ListData { get; set; } = new List<ProductivityDetailRespone>();
    }
    public class ProductivityDetailRespone
    {
        public string RouteZone { get; set; }
        public string InventoryId { get; set; }
        public string Description { get; set; }
        public string TerritoryStrID { get; set; }
        public string TerritoryValueKey { get; set; }
        public int SLThungOrder { get; set; }
        public int SLLocOrder { get; set; }
        public int SLChaiOrder { get; set; }
        public int SLThungShipped { get; set; }
        public int SLLocShipped { get; set; }
        public int SLChaiShipped { get; set; }
    }

    public class ProductivityReportSalesTerritoryRespone
    {
        public string TerritoryLevelValues { get; set; }
        public string TerritoryValeDescription { get; set; }
        public List<ProductivityTerritoryValueDetailRespone> ListData { get; set; } = new List<ProductivityTerritoryValueDetailRespone>();
    }
    public class ProductivityTerritoryValueDetailRespone
    {
        public string TerritoryLevelValues { get; set; }
        public string InventoryId { get; set; }
        public string Description { get; set; }
        public string BaseUom { get; set; }
        public int OrderSKUQty { get; set; }
        public int ShippedSKUQty { get; set; }
    }

    public class TerritoryMappingByValueModel
    {
        public string MappingNode { get; set; }
        public string TerritoryValueKey { get; set; }
        public string TerritoryValueDescription { get; set; }
        public string ParentMappingNode { get; set; }
        public string TerritoryStructureCode { get; set; }
        public int Level { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? UntilDate { get; set; }
        public string SaleOrgCode { get; set; }
        public List<TerritoryMappingByValueModel> ListChildren { get; set; }
    }

    public class TerritoryStructureModel
    {
        public Guid? Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? UntilDate { get; set; }
        public virtual List<TerritoryStructureDetailModel> TerritoryStructureDetails { get; set; }
    }

    public class TerritoryStructureDetailModel
    {
        public Guid? Id { get; set; }
        public string TerritoryStructureCode { get; set; }
        public string Description { get; set; }
        public int Level { get; set; }
        public string TerritoryLevelCode { get; set; }
    }

    // Report 13
    // Flow distributor
    public class ProductivityByDayReportRouteZoneRespone
    {
        public string RouteZoneCode { get; set; }
        public List<ProductivityByDayReportDetailRespone> ListData { get; set; } = new List<ProductivityByDayReportDetailRespone>();
    }

    public class ProductivityByDayReportDetailRespone
    {
        public string TerritoryLevelValues { get; set; }
        public string InventoryId { get; set; }
        public string Description { get; set; }
        public List<LineProductivityByDayResponse> ListData { get; set; } = new List<LineProductivityByDayResponse>();
    }

    public class LineProductivityByDayRes
    {
        public DateTime OrderDate { get; set; }
        public int ShippedSKUQty { get; set; }
    }
    public class LineProductivityByDayResponse
    {
        public DateTime OrderDate { get; set; }
        public int QuantityThung { get; set; }
        public int QuantityLoc { get; set; }
        public int QuantityChai { get; set; }
    }

    // Flow HO
    public class ProductivityByDayReportTerritoryValueRespone
    {
        public string TerritoryLevelValues { get; set; }
        public string TerritoryValeDescription { get; set; }
        public string RouteZoneCode { get; set; }
        public List<ProductivityByDayReportDetailRespone> ListData { get; set; } = new List<ProductivityByDayReportDetailRespone>();
    }
    public class IsInSubRouteModel
    {
        public bool IsInSubRoute { get; set; }
    }

    // SO.RP07 ProductivityBySalesReport
    public class ProductivityBySalesReportRequest : FromDateToDateNullableModel
    {
        [Required]
        public string IntemHierarchyLevel { get; set; }
        public string SalesTerritoryValue { get; set; }
        public string DistributorCode { get; set; }
        public string TerritoryStructureCode { get; set; }
        public string TerritoryValue { get; set; }
        public string RouteZoneCode { get; set; }
        public string SaleOrgCode { get; set; }

    }

    public class ProductivityBySalesReportResult
    {
        public int Level { get; set; }
        public string HierarchyLevelValue { get; set; }
        public List<PBSReportRouteZoneList> GroupedRouteZone { get; set; }
        public List<PBSGoupedTerritoryLevel> GoupedTerritoryLevel { get; set; }
    }

    public class ProductivityBySalesReportResultV2
    {
        public int Level { get; set; }
        public string HierarchyLevelValue { get; set; }
        public List<PBSReportRouteZoneListV2> GroupedRouteZone { get; set; }
        public List<PBSGoupedTerritoryLevelV2> GoupedTerritoryLevel { get; set; }
    }

    public class PBSReportRouteZoneList
    {
        public string RoutezoneCode { get; set; }
        public int Quantities { get; set; }
    }

    public class PBSReportRouteZoneListV2
    {
        public string RoutezoneCode { get; set; }
        public int ShippedSLThung { get; set; }
        public int ShippedSLLoc { get; set; }
        public int ShippedSLChai { get; set; }
    }

    public class PBSGoupedTerritoryLevel
    {
        public string TerritoryLevelKey { get; set; }
        public int Quantities { get; set; }
    }

    public class PBSGoupedTerritoryLevelV2
    {
        public string TerritoryLevelKey { get; set; }
        public int ShippedSLThung { get; set; }
        public int ShippedSLLoc { get; set; }
        public int ShippedSLChai { get; set; }
    }

    //SO.RP08 
    public class PBPReportRequest : FromDateToDateNullableModel
    {
        [Required]
        public string ItemHierarchyLevel { get; set; }
        public string SalesTerritoryValue { get; set; }
        public string DistributorCode { get; set; }
        public string TerritoryStructureCode { get; set; }
        public string TerritoryValue { get; set; }
        public string ViewBy { get; set; }
        public string SaleOrgCode { get; set; }
        public string RouteZoneCode { get; set; }

    }
    public class PBPReportHierarchyLevelValue
    {
        public int Level { get; set; }
        public string HierarchyLevelValue { get; set; }
        public int Quantities { get; set; }
        public string InventoryAttibute { get; set; }
    }

    public class PBPReportHierarchyLevelValueV2
    {
        public int Level { get; set; }
        public string HierarchyLevelValue { get; set; }
        public int ShippedSLThung { get; set; }
        public int ShippedSLLoc { get; set; }
        public int ShippedSLChai { get; set; }
        public string InventoryAttibute { get; set; }
    }

    public class PBPReportResult
    {
        public string RoutezoneCode { get; set; }
        public string DSACode { get; set; }
        public List<PBPReportHierarchyLevelValue> DataValues { get; set; }
    }

    public class PBPReportResultV2
    {
        public string RoutezoneCode { get; set; }
        public string DSACode { get; set; }
        public List<PBPReportHierarchyLevelValueV2> DataValues { get; set; }
    }

    public class GetListDistributorModel
    {
        public string TerritoryValue { get; set; }
        public string TerritoryStructureCode { get; set; }
        public string SaleOrgCode { get; set; }
    }
    #endregion

    #region ProcessPendingDataTrans
    public class OrderPendingTransModel
    {
        public DateTime BaselineDate { get; set; }
        public List<OrderPendingTransDetailModel> Detail { get; set; }
    }
    public class ODOrderPendingTransModel
    {
        public string DistributorCode { get; set; }
        public DateTime BaselineDate { get; set; }
        public List<OrderPendingTransDetailModel> Detail { get; set; }
    }
    public class OrderPendingTransDetailModel
    {
        public string FromStatus { get; set; }
        public string ToStatus { get; set; }
    }
    #endregion

    #region External Model

    public class ExQueryDataForPromotion
    {
        public SO_OrderItems td { get; set; }
        public SO_OrderInformations th { get; set; }
    }
    public class ExPromotionReportEcoParameters : EcoParameters
    {
        public string PromotionCode { get; set; }
        public string PromotionLevelCode { get; set; }
        public DateTime EffectiveDateFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public string SaleOrg { get; set; }
        public string ScopeType { get; set; }
        public string ApplicableObjectType { get; set; }
        public List<string> ListApplicableObject { get; set; }
        public List<string> ListScope { get; set; }
        public List<string> ListCustomer { get; set; }
        public List<string> ListRouteZone { get; set; }
        public List<string> ListOrder { get; set; }
    }

    public class PromotionDetailReportOrderListModel
    {
        public string OrdNbr { get; set; }
        public DateTime OrdDate { get; set; }
        public string PromotionLevel { get; set; }
        public string InventoryID { get; set; }
        public string InventoryName { get; set; }
        public decimal? Shipped_Qty { get; set; }
        public string PackSize { get; set; }
        public decimal? ShippedLineDiscAmt { get; set; }
        public string CustomerID { get; set; }
        public string ShiptoID { get; set; }
        public string ShiptoName { get; set; }
        public string ReferenceLink { get; set; }
        public string SalesRepCode { get; set; }
    }

    public class ListPromotionDetailReportOrderListModel
    {
        public List<PromotionDetailReportOrderListModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }


    public class PromotionDetailReportRouteZoneListModel
    {
        public string RouteZoneId { get; set; }
        public string RouteZoneDescription { get; set; }
        public string PromotionLevel { get; set; }
        public string PromotionLevelName { get; set; }
        public string SalesRepCode { get; set; }
        public string ReferenceLink { get; set; }
    }

    public class ListPromotionDetailReportRouteZoneListModel
    {
        public List<PromotionDetailReportRouteZoneListModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }

    public class PromotionDetailReportPointSaleListModel
    {
        public string CustomerID { get; set; }
        public string CustomerName { get; set; }
        public string ShiptoID { get; set; }
        public string ShiptoName { get; set; }
        public string PromotionLevel { get; set; }
        public string PromotionLevelName { get; set; }
        public string ReferenceLink { get; set; }
        public string SalesRepCode { get; set; }
    }

    public class ListPromotionDetailReportPointSaleListModel
    {
        public List<PromotionDetailReportPointSaleListModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }
    #endregion

    #region Khoa enhance
    public class SaleOrderDetailRequestModel
    {
        public string ExternalOrdNBR { get; set; }
        public string OrderType { get; set; }
    }
    #endregion
}
