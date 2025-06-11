using System;

namespace ODSaleOrder.API.Infrastructure.SOInfrastructure
{
    public partial class FfasoOrderItem
    {
        public Guid Id { get; set; }
        public string OrderRefNumber { get; set; }
        public string VisitId { get; set; }
        public string External_OrdNBR { get; set; }
        public string InventoryID { get; set; }
        public bool? IsKit { get; set; }
        public string LocationID { get; set; }
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        public string UOM { get; set; }
        public string UOMDesc { get; set; }
        public double? UnitRate { get; set; }
        public double? OrigUnitPrice { get; set; }
        public double? UnitPrice { get; set; }
        public string ItemGroupId { get; set; }
        public string ItemGroupDescription { get; set; }
        public int? DemandOrderQty { get; set; }
        public int? OriginalOrderQty { get; set; }
        public int? OriginalOrderBaseQty { get; set; }
        public int? OriginalOrderQtyBooked { get; set; }
        public string AllocateType { get; set; }
        public double? VatValue { get; set; }
        public string VATCode { get; set; }
        public double? VAT { get; set; }
        public bool? IsFree { get; set; }
        public double? Orig_Ord_Line_Amt { get; set; }
        public double? Orig_Ord_line_Disc_Amt { get; set; }
        public double? Orig_Ord_Line_Extend_Amt { get; set; }
        public string DiscountType { get; set; }
        public double? DisCountAmount { get; set; }
        public double? DiscountPercented { get; set; }
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
        public string ItemGroupCode { get; set; }
        public string ItemGroupName { get; set; }
        public int? ItemPoint { get; set; }
        public string KitKey { get; set; }
        public string ProgramCustomersDetailCode { get; set; }
        public string ItemShortName { get; set; }
        public string BaseUnitCode { get; set; }
        public string PurchaseUnitCode { get; set; }
        public string RewardDescription { get; set; }
        public string SalesUnitCode { get; set; }
        public string SaleUnitDescription { get; set; }
        public bool? IsBudget { get; set; }
        public bool? BudgetCheckStatus { get; set; }
        public string BudgetCode { get; set; }
        public string BudgetType { get; set; }
        public int? BudgetDemand { get; set; }
        public int? BudgetBooked { get; set; }
        public int? BudgetBook { get; set; }
        public bool? BudgetBookOver { get; set; }
        public string BudgetBookOption { get; set; }
        public bool? StockCheckStatus { get; set; }
        public bool? RequiredCheckStock { get; set; }
        public bool? PendingOrder { get; set; }
        public bool? WaittingStock { get; set; }
        public string KitId { get; set; }
        public string KitName { get; set; }
        public int? KitQuantity { get; set; }
        public double? KitAmount { get; set; }
        public string KitUomId { get; set; }
        public string KitUomName { get; set; }
        public double? KitUnitPrice { get; set; }
        public int? AllowChangeSKU { get; set; }
        public string PromotionType { get; set; }
        public string PromotionCode { get; set; }
        public string PromotionDescription { get; set; }
        public string PromotionLevelCode { get; set; }
        public string PromotionLevelDescription { get; set; }
        public int? NumberPickedLevel { get; set; }
        public string PromotionSchemeID { get; set; }
        public string PromotionDealID { get; set; }
        public string PromotionOrderRule { get; set; }
        public string PromotionOrderType { get; set; }
        public bool? PromotionRuleofGiving { get; set; }
        public double? PromotionOrderQty { get; set; }
        public double? PromotionProductAmount { get; set; }
        public double? PromotionProductQty { get; set; }
        public string SaleStep { get; set; }
        public bool? IsDeleted { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public bool? BudgetImport { get; set; }
        public bool? StockImport { get; set; }
        public int? OriginalOrderBaseQtyBooked { get; set; }
        public bool? IsDirect { get; set; }
        public string? OrderType { get; set; }

        //Enhance Calculate Tax
        public double? Orig_Ord_Line_TaxBefore_Amt { get; set; }
        public double? Orig_Ord_Line_TaxAfter_Amt { get; set; }

        //
        public double? UnitPriceBeforeTax { get; set; } = 0;
        public double? UnitPriceAfterTax { get; set; } = 0;
        public double? Orig_Ord_TotalLine_Disc_Amt { get; set; } = 0;

        //
        public string? StockBookingId { get; set; } 
    }
}
