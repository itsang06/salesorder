using System;
using System.Collections.Generic;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;

namespace ODSaleOrder.API.Models
{
    public class SumpickingModel : SO_SumPickingListHeader
    {
        public List<SO_SumPickingListDetail> SumPickingListDetails { get; set; }
    }

    public class SumpickingDetailModel
    {
        public SO_SumPickingListHeader sumPickingListHeader { get; set; }
        public List<SO_SumPickingListDetail> SumPickingListDetails { get; set; }
        public List<SumpickingItemModel> SumpickingItems { get; set; }
        public List<SO_OrderItems> PrintItems { get; set; }

    }

    public class SumpickingItemDetailModel
    {
        public List<SumpickingItemModel> SumpickingItem { get; set; }
        public List<SO_OrderItems> PrintItems { get; set; }
    }

    public class SumpickingItemModel
    {
        public string InventoryID { get; set; }
        public string Description { get; set; }
        public int Orig_Ord_Qty { get; set; }
        public int Ord_Qty { get; set; }
        public int Shipped_Qty { get; set; }
        public int OrderBaseQuantities { get; set; }
        public int ShippingQuantities { get; set; }
        public int FailedQuantities { get; set; }
        public int RemainQuantities { get; set; }
        public int Sales_Orig_Ord_Qty { get; set; }
        public int Sales_Ord_Qty { get; set; }
        public int Sales_Shipped_Qty { get; set; }
        public int Sales_OrderBaseQuantities { get; set; }
        public int Sales_ShippingQuantities { get; set; }
        public int Sales_FailedQuantities { get; set; }
        public int Sales_RemainQuantities { get; set; }
        public Guid? PurchaseUnit { get; set; } = Guid.Empty;
        public string SalesUnit { get; set; }
        public string BaseUnit { get; set; }


    }

    public class SumpickingDetailQueryModel
    {
        public string SumPickingRefNumber { get; set; }
        public string DistributorCode { get; set; }
    }
    public class SumpickingDetailQueryModelV2 : SumpickingDetailQueryModel
    {
        public List<string> SumPickingRefNumberList { get; set; }
    }

    public class SumpickingSearchModel : EcoparamsWithGenericFilter
    {
        public string SumPickingRefNumber { get; set; }
        public string DistributorCode { get; set; }
        public List<string> SaleOrders { get; set; }
    }

    public class SumpickingDetailItemQueryModel
    {
        public List<string> OrderRefNumbers { get; set; }
        public string DistributorCode { get; set; }
    }

    public class ListSumpickingModel
    {
        public List<SumpickingModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }

    #region SumPickingReport
    public class SumPickingHeaderReportModel
    {
        public string SumPickingRefNumber { get; set; }
        public int PrintedCount { get; set; }
        public string DistributorCode { get; set; }
        public string DistributorName { get; set; }
        public string AttentionPhoneValue { get; set; }
        public string BussinessFullAddress { get; set; }
        public string LogoFilePath { get; set; }
        public string Vehicle { get; set; }
        public string VehicleLoad { get; set; }
        public string DriverCode { get; set; }
        public string DeliveryName { get; set; }        
        public string DeliveryPhone { get; set; }        
        public DateTime CreatedDate { get; set; }
        
    }
    public class SumPickingSalesmanReportModel
    {
        public string RouteZoneCode { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeePhone { get; set; }
        public string SumPickingRefNumber { get; set; } = string.Empty;

    }
    public class SumPickingInventoryReportModel
    {
        public string InventoryCode { get; set; }
        public string InventoryName { get; set; }
        public int OrderSLThung { get; set; }
        public int OrderSLLoc { get; set; }
        public int OrderSLChai { get; set; }
        public int ShippedSLThung { get; set; }
        public int ShippedSLLoc { get; set; }
        public int ShippedSLChai { get; set; }
        public int FailedSLThung { get; set; }
        public int FailedSLLoc { get; set; }
        public int FailedSLChai { get; set; }
        public string SumPickingRefNumber { get; set; } = string.Empty;

    }
    public class SumPickingSalesOrderReportModel
    {
        public string SumPickingRefNumber { get; set; } = string.Empty;
        public string OrderRefNumber { get; set; }
        public string CustomerName { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeePhone { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Ord_Extend_Amt { get; set; }
        public string Note { get; set; }
        public bool IsPrintedDeliveryNote { get; set; }

    }
    public class SumPickingListReportModel
    {
        public SumPickingHeaderReportModel sumPickingHeaderReports { get; set; } = new SumPickingHeaderReportModel();
        public List<SumPickingSalesmanReportModel> sumPickingSalesmanReports { get; set; } = new List<SumPickingSalesmanReportModel>();
        public List<SumPickingInventoryReportModel> sumPickingInventoryReports { get; set; } = new List<SumPickingInventoryReportModel>();
        public List<SumPickingSalesOrderReportModel> sumPickingSalesOrderReports { get; set; } = new List<SumPickingSalesOrderReportModel>();
    }
    #endregion
}
