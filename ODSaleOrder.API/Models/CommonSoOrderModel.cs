using System;
namespace ODSaleOrder.API.Models
{
  public  class CommonSoOrderModel 
  {
    public Guid Id { get; set; }
    public string DistributorCode { get; set; }
    public string OrderRefNumber { get; set; }//SONumber
    public string Status { get; set; }
    public string SalesRepID { get; set; }
    public string CustomerId { get; set; }//CustomerCode
    public string CustomerName { get; set; }//CustomerName
    public DateTime? OrderDate { get; set; }
    public string RouteZoneID { get; set; } //RouteZoneCode
    public decimal TotalVAT { get; set; } //VAT
    public decimal Shipped_Disc_Amt { get; set; } //DiscountAmount
    public string CustomerShiptoID { get; set; }
    public string OrderType { get; set; } // SaleOrder hoặc ReturnOrder để làm màn hình So.06 
    public string ReasonCode { get; set; } //so.3 
    public bool IsDirect { get; set; }
    public int TotalLine { get; set; } // Đếm sp có trên đơn hàng (Groupby theo ItemCode)
    public bool IsPrintedDeliveryNote { get; set; }
    public string ReferenceRefNbr { get; set; }
    public int Shipped_Qty { get; set; } //Tổng sản lượng giao thành công trên đơn hàng
    public int Ord_Qty { get; set; } //Tổng sản lượng xác nhận đặt trên đơn hàng
    public string TerritoryValueKey { get; set; }
    public string DSAID { get; set; }
    public decimal Shipped_Extend_Amt { get; set; } //Tổng tiền sau CK và KM được giao thành công
    public decimal Ord_Extend_Amt { get; set; } //Tổng tiền sau CK và KM được xác nhận
    public decimal Shipped_Amt { get; set; } //Tổng doanh số được giao thành công trên đơn hàng
    public decimal Ord_Amt { get; set; } //Tổng doanh số được xác nhận trên đơn ahfng

    ///Detail: 
    public string InventoryID { get; set; }
    public string ItemDescription { get; set; }
    public string UOM { get; set; }
    public int? OrderBaseQuantities { get; set; } //OrderQTY
    public int? ShippedBaseQuantities { get; set; } //ActualShippedQTY or Quantities
    public string InventoryAttibute1 { get; set; }
    public string InventoryAttibute2 { get; set; }
    public string InventoryAttibute3 { get; set; }
    public string InventoryAttibute4 { get; set; }
    public string InventoryAttibute5 { get; set; }
    public string InventoryAttibute6 { get; set; }
    public string InventoryAttibute7 { get; set; }
    public string InventoryAttibute8 { get; set; }
    public string InventoryAttibute9 { get; set; }
    public string InventoryAttibute10 { get; set; }
    public string ItemCode { get; set; }
    public Guid? BaseUnit { get; set; }
    public string ItemGroupCode { get; set; }
    public bool? IsFree { get; set; } = false;
    public decimal? Ord_Line_Amt { get; set; } = 0;
  }
}
