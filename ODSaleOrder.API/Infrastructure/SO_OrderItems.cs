using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class SO_OrderItems : AuditTable
    {
        public Guid Id { get; set; } = Guid.Empty;
        [MaxLength(50)] public string OrderRefNumber { get; set; }
        [MaxLength(50)] public string InventoryID { get; set; }
        [MaxLength(50)] public string KitKey { get; set; }
        public Guid KitId { get; set; } = Guid.Empty;
        public bool IsKit { get; set; } = false;
        // public bool IsPromotion { get; set; } = false;
        [MaxLength(50)] public string LocationID { get; set; }
        public Guid ItemId { get; set; } = Guid.Empty;
        [MaxLength(50)] public string ItemCode { get; set; }
        [MaxLength(250)] public string ItemDescription { get; set; }
        [MaxLength(50)] public string UOM { get; set; }
        public int UnitRate { get; set; } = 0; //Tỉ lệ chuyển đổi của UOM với đơn vị lưu kho Base
        public int OriginalOrderQuantities { get; set; } = 0;  // Số lượng KH muốn đặt
        public int OriginalOrderBaseQuantities { get; set; } = 0; //quy đổi số lượng base cho số lượng kh muốn đặt
        public int OrderQuantities { get; set; } = 0; // số lượng thực tế npp có thể giao
        public int OrderBaseQuantities { get; set; } = 0; // quy đổ số lượng base cho số lượng thực tế npp có thể giao
        public int ShippedQuantities { get; set; } = 0;
        public int ShippedBaseQuantities { get; set; } = 0; // quy đổi số lượng base cho ShippedQuantities
        public int FailedQuantities { get; set; } = 0;
        public int FailedBaseQuantities { get; set; } = 0;
        public int ShippingQuantities { get; set; } = 0;
        public int ShippingBaseQuantities { get; set; } = 0;
        public int RemainQuantities { get; set; } = 0;
        public int ReturnQuantities { get; set; } = 0;
        public int ReturnBaseQuantities { get; set; } = 0;
        public decimal VAT { get; set; }
        public decimal VatValue { get; set; } = 0;
        [MaxLength(50)] public string VATCode { get; set; }
        public bool IsFree { get; set; } = false;
        public decimal UnitPrice { get; set; } = 0;
        [MaxLength(250)] public string PromotionCode { get; set; }
        [MaxLength(250)] public string ProgramCustomersDetailCode { get; set; }
        [MaxLength(250)] public string ProgramCustomersDetailDesc { get; set; }
        [MaxLength(250)] public string PromotionDescription { get; set; }
        [MaxLength(250)] public string PromotionType { get; set; } // Dispay , Accumulate , Promotion 

        // khuyến mãi
        [MaxLength(50)] public string DiscountID { get; set; }
        [MaxLength(250)] public string DiscountType { get; set; }
        [MaxLength(50)] public string DiscountSchemeID { get; set; }
        [MaxLength(50)] public string DiscountDealID { get; set; }
        public double DiscountPercented { get; set; }
        public decimal DisCountAmount { get; set; }
        // Các loại giá tổng kết cho từng item theo từng trạng thái của đơn hàng
        public decimal Orig_Ord_Line_Amt { get; set; } = 0;  //Số tiền  ban đầu
        public decimal Ord_Line_Amt { get; set; } = 0;  //Số tiền khi xác nhận đơn hàng
        public decimal Shipped_Line_Amt { get; set; } = 0;  //Số tiền khi giao đơn hàng
        public decimal Orig_Ord_line_Disc_Amt { get; set; } = 0;  //Số tiền KM ban đầu
        public decimal Ord_line_Disc_Amt { get; set; } = 0;  //Số tiền KM khi xác nhận đơn hàng
        public decimal Shipped_line_Disc_Amt { get; set; } = 0;  //Số tiền KM khi giao đơn hàng
        public decimal Orig_Ord_Line_Extend_Amt { get; set; } = 0;  //Tiền sau CK và KM ban đầu
        public decimal Ord_Line_Extend_Amt { get; set; } = 0;  //Tiền sau CK và KM được xác nhận
        public decimal Shipped_Line_Extend_Amt { get; set; } = 0;  //Tiền sau CK và KM được giao thành công
        // Item attribute
        [MaxLength(50)] public string InventoryAttibute1 { get; set; }
        [MaxLength(50)] public string InventoryAttibute2 { get; set; }
        [MaxLength(50)] public string InventoryAttibute3 { get; set; }
        [MaxLength(50)] public string InventoryAttibute4 { get; set; }
        [MaxLength(50)] public string InventoryAttibute5 { get; set; }
        [MaxLength(50)] public string InventoryAttibute6 { get; set; }
        [MaxLength(50)] public string InventoryAttibute7 { get; set; }
        [MaxLength(50)] public string InventoryAttibute8 { get; set; }
        [MaxLength(50)] public string InventoryAttibute9 { get; set; }
        [MaxLength(50)] public string InventoryAttibute10 { get; set; }
        [MaxLength(50)] public string ItemGroupCode { get; set; }
        public decimal ItemPoint { get; set; } = 0;
        public Guid? BaseUnit { get; set; } = Guid.Empty;
        [MaxLength(50)] public string BaseUnitCode { get; set; }
        public Guid? SalesUnit { get; set; } = Guid.Empty;
        [MaxLength(50)] public string SalesUnitCode { get; set; }
        public Guid? PurchaseUnit { get; set; } = Guid.Empty;
        [MaxLength(50)] public string PurchaseUnitCode { get; set; }
        public Guid? VatId { get; set; } = Guid.Empty;
        public Guid? Hierarchy { get; set; } = Guid.Empty;
        [MaxLength(250)] public string ItemShortName { get; set; }
        [MaxLength(50)] public string BaseUomCode { get; set; }
        [MaxLength(250)] public string RewardDescription { get; set; }
        [MaxLength(250)] public string PromotionLevel { get; set; }
        [MaxLength(50)] public string UOMDesc { get; set; }
        [MaxLength(100)] public string AllocateType { get; set; }
        [MaxLength(100)] public string OwnerType { get; set; }
        [MaxLength(255)] public string OwnerCode { get; set; }

        //Enhance Calculate Tax
        public double? Ord_Line_TotalBeforeTax_Amt { get; set; } = 0; //Doanh số trước thuế được xác nhận trên line hàng
        public double? Ord_Line_TotalAfterTax_Amt { get; set; } = 0; //Doanh số sau thuế được xác nhận trên line hàng
        public double? Shipped_Line_TaxBefore_Amt { get; set; } = 0; //Tiền trước thuế được giao trên line hàng
        public double? Shipped_Line_TaxAfter_Amt { get; set; } = 0; //Tiền sau thuế được giao trên line hàng
       

        //
        public double? Ord_TotalLine_Disc_Amt {  get; set; } = 0;
        public double? OrgUnitPrice { get; set; } = 0;

        //
        public double? UnitPriceBeforeTax { get; set; } = 0;
        public double? UnitPriceAfterTax { get; set; } = 0;

        //
        public string PromotionBudgetCode { get; set; } //Mã Suất CTKM apply cho line hàng
        public int? PromotionBudgetQuantities { get; set; } = 0!; //Suất CTKM apply cho line hàng
    }
}
