using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class SO_OrderInformations : AuditTable
    {
        public Guid Id { get; set; }
        [MaxLength(50)] public string OrderRefNumber { get; set; }
        [MaxLength(250)] public string OrderDescription { get; set; }
        [MaxLength(50)] public string ReferenceRefNbr { get; set; } // nếu là Return thì nó tham chiếu tới SO trước đó 
        [MaxLength(250)] public string CancelNumber { get; set; } //so.3 
        [MaxLength(50)] public string ReasonCode { get; set; } //so.3 
        public DateTime? CancelDate { get; set; } //so.3 
        public bool NotInSubRoute { get; set; }
        public bool IsDirect { get; set; }
        [MaxLength(50)] public string OrderType { get; set; } // SaleOrder hoặc ReturnOrder để làm màn hình So.06 
        [MaxLength(50)] public string PeriodID { get; set; } //Dựa vào ngày đơn hàng mà lấy được chu kỳ bán hàng ở sales calendar 
        [MaxLength(50)] public string WareHouseID { get; set; }
        [MaxLength(50)] public string PrincipalID { get; set; }  //Mã Principal 
        [MaxLength(50)] public string DistributorCode { get; set; }
        [MaxLength(50)] public string Disty_billtoID { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public bool isReturn { get; set; }
        [MaxLength(50)] public string Status { get; set; } //"SO_ST_DRAFT","SO_ST_OPEN","SO_ST_SHIPPING","SO_ST_WAITINGSHIPPING","SO_ST_DELIVERED","SO_ST_PARTIALDELIVERED","SO_ST_FAILED"

        //So.05
        public bool IsPrintedDeliveryNote { get; set; }
        public int PrintedDeliveryNoteCount { get; set; }
        public DateTime? LastedDeliveryNotePrintDate { get; set; }
        //

        //SF Assignment
        [MaxLength(50)] public string SalesOrgID { get; set; }
        [MaxLength(50)] public string TerritoryStrID { get; set; }
        [MaxLength(50)] public string TerritoryValueKey { get; set; }
        [MaxLength(50)] public string BranchId { get; set; }
        [MaxLength(50)] public string RegionId { get; set; }
        [MaxLength(50)] public string SubRegionId { get; set; }
        [MaxLength(50)] public string AreaId { get; set; }
        [MaxLength(50)] public string SubAreaId { get; set; }
        [MaxLength(50)] public string DSAID { get; set; }
        [MaxLength(50)] public string NSD_ID { get; set; }
        [MaxLength(50)] public string Branch_Manager_ID { get; set; }
        [MaxLength(50)] public string Region_Manager_ID { get; set; }
        [MaxLength(50)] public string Sub_Region_Manager_ID { get; set; }
        [MaxLength(50)] public string Area_Manager_ID { get; set; }
        [MaxLength(50)] public string Sub_Area_Manager_ID { get; set; }
        [MaxLength(50)] public string DSA_Manager_ID { get; set; }
        [MaxLength(50)] public string RZ_Suppervisor_ID { get; set; }
        [MaxLength(50)] public string SIC_ID { get; set; }
        [MaxLength(50)] public string SalesRepID { get; set; }

        //Shipto Info
        [MaxLength(50)] public string RouteZoneID { get; set; }
        [MaxLength(50)] public string RouteZOneType { get; set; }
        [MaxLength(50)] public string RouteZonelocation { get; set; }
        [MaxLength(50)] public string CustomerId { get; set; }
        [MaxLength(50)] public string CustomerShiptoID { get; set; }
        [MaxLength(250)] public string CustomerPhone { get; set; }
        [MaxLength(250)] public string CustomerName { get; set; }
        [MaxLength(250)] public string CustomerAddress { get; set; }
        [MaxLength(50)] public string Shipto_Attribute1 { get; set; }
        [MaxLength(50)] public string Shipto_Attribute2 { get; set; }
        [MaxLength(50)] public string Shipto_Attribute3 { get; set; }
        [MaxLength(50)] public string Shipto_Attribute4 { get; set; }
        [MaxLength(50)] public string Shipto_Attribute5 { get; set; }
        [MaxLength(50)] public string Shipto_Attribute6 { get; set; }
        [MaxLength(50)] public string Shipto_Attribute7 { get; set; }
        [MaxLength(50)] public string Shipto_Attribute8 { get; set; }
        [MaxLength(50)] public string Shipto_Attribute9 { get; set; }
        [MaxLength(50)] public string Shipto_Attribute10 { get; set; }
        public DateTime? ExpectShippedDate { get; set; }
        public DateTime OrderDate { get; set; }

        public DateTime? VisitDate { get; set; }//Thông tin ngày đặt hàng ( có dữ liệu khi đơn hàng được lấy từ Mobile)
        [MaxLength(50)] public string VisitID { get; set; } //Thông tin ngày chăm sóc KH ( có dữ liệu khi đơn hàng được lấy từ Mobile)
        [MaxLength(50)] public string External_OrdNBR { get; set; } //Mã đơn hàng được lấy từ mobile gửi về
        [MaxLength(50)] public string Owner_ID { get; set; } //Mã người thụ hưởng đơn hàng
        [MaxLength(50)] public string Source { get; set; } //Nguồn đơn hàng Mobile / Không phải mobile

        //Field tính tổng
        public int Orig_Ord_SKUs { get; set; } //Số SP ban đầu trên đơn hàng
        public int Ord_SKUs { get; set; } //Số SP trên đơn hàng được xác nhận
        public int Shipped_SKUs { get; set; } //Số sản phẩm giao thành công
        public int Orig_Ord_Qty { get; set; } //Tổng sản lượng đặt ban đầu trên đơn hàng
        public int Ord_Qty { get; set; } //Tổng sản lượng xác nhận đặt trên đơn hàng
        public int Shipped_Qty { get; set; } //Tổng sản lượng giao thành công trên đơn hàng
        public int Orig_Promotion_Qty { get; set; } //Tổng sản lượng KM ban đầu trên đơn hàng
        public int Promotion_Qty { get; set; } //Tổng sản lượng KM được xác nhận trên đơn hàng
        public int Shipped_Promotion_Qty { get; set; } //Tổng sản lượng KM được giao thành công trên dơn hàng
        public decimal Orig_Ord_Amt { get; set; } //Tổng doanh số đặt ban đầu trên đơn hàng
        public decimal Ord_Amt { get; set; } //Tổng doanh số được xác nhận trên đơn ahfng
        public decimal Shipped_Amt { get; set; } //Tổng doanh số được giao thành công trên đơn hàng
        public decimal Promotion_Amt { get; set; } //Tổng tiền khuyến mãi
        public decimal Shipped_Promotion_Amt { get; set; } //Tổng tiền khuyến mãi khi xác nhận giao hàng
        public decimal Orig_Ord_Disc_Amt { get; set; } //Tổng tiền CK ban đầu trên ĐH
        public decimal Ord_Disc_Amt { get; set; } //Tổng tiền CK trên ĐH được xác nhận
        public decimal Shipped_Disc_Amt { get; set; } //Tổng tiền CK giao thành công trên ĐH
        public decimal Orig_Ordline_Disc_Amt { get; set; } //Tổng tiền KM ban đầu trên ĐH
        public decimal Ordline_Disc_Amt { get; set; } //Tổng tiền KM trên ĐH được xác nhận
        public decimal Shipped_line_Disc_Amt { get; set; } //Tổng tiền KM giao thành công trên ĐH
        public decimal Orig_Ord_Extend_Amt { get; set; } //Tổng tiền sau CK và KM ban đầu
        public decimal Ord_Extend_Amt { get; set; } //Tổng tiền sau CK và KM được xác nhận
        public decimal Shipped_Extend_Amt { get; set; } //Tổng tiền sau CK và KM được giao thành công
        public decimal TotalVAT { get; set; } //Tổng số thuế

        public int ConfirmCount { get; set; }
        [MaxLength(50)] public string PromotionRefNumber { get; set; }
        [MaxLength(50)] public string MenuType { get; set; }
        public DateTime? ExpectDeliveryNote { get; set; }
        [MaxLength(250)] public string Note { get; set; }
        public int TotalLine { get; set; } // Đếm sp có trên đơn hàng (Groupby theo ItemCode)
        [MaxLength(250)] public string CustomerShiptoName { get; set; }

        public DateTime? ShipDate { get; set; }
        public DateTime? CompleteDate { get; set; }
        public string DiscountID { get; set; }

        [MaxLength(255)] public string SalesRepName { get; set; }

        // Add field Owner
        [MaxLength(100)] public string OwnerType { get; set; }
        [MaxLength(255)] public string OwnerCode { get; set; }

        // Add field GEO
        public string CusAddressCountryId { get; set; }
        public string CusAddressProvinceId { get; set; }
        public string CusAddressDistrictId { get; set; }
        public string CusAddressWardId { get; set; }

        public string OSOutletCode { get; set; }
        public string DistributorName { get; set; }
        [NotMapped]
        public string OSStatus { get; set; }

        [NotMapped]
        public string SOStatus { get; set; }
        public string RouteZoneName { get; set; }

        //Enhance Calculate Tax
        public double? Ord_TotalBeforeTax_Amt { get; set; } = 0; //Tổng doanh số trước thuế được xác nhận trên đơn hàng =>  sum Ord_Line_TotalBeforeTax_Amt
        public double? Ord_TotalAfterTax_Amt { get; set; } = 0; //Tổng doanh số sau thuế được xác nhận trên đơn hàng =>	sum Ord_Line_TotalAfterTax_Amt
        public double? Shipped_TotalBeforeTax_Amt { get; set; } = 0; //Tổng doanh số trước thuế được giao thành công =>  sum Shipped_Line_TaxBefore_Amt
        public double? Shipped_TotalAfterTax_Amt { get; set; } = 0; //Tổng doanh số sau thuế được giao thành công => sum Shipped_Line_TaxAfter_Amt

        //Enhance Return SalesOrder
        public string? PromotionRetention { get; set; }

        // Add field for error message process pending data
        public string? ErrorMessage { get; set; }
    }
}
