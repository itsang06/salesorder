using System;

namespace ODSaleOrder.API.Infrastructure
{
    public partial class OsOrderItem
    {
        public Guid Id { get; set; }

        public string OrderRefNumber { get; set; }

        public string ExternalOrdNbr { get; set; }

        public string ItemCode { get; set; }

        public string ItemShortName { get; set; }

        public string ItemDescription { get; set; }

        public string ItemGroupCode { get; set; }

        public string ItemGroupName { get; set; }

        public string ItemGroupDescription { get; set; }

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

        public bool? IsKit { get; set; }

        public string KitId { get; set; }

        public string KitName { get; set; }

        public int? KitQuantity { get; set; }

        public double? KitAmount { get; set; }

        public string KitUomId { get; set; }

        public string KitUomName { get; set; }

        public double? KitUnitPrice { get; set; }

        public string BaseUnitCode { get; set; }

        public string PurchaseUnitCode { get; set; }

        public string SalesUnitCode { get; set; }

        public string SaleUnitDescription { get; set; }

        public string Uom { get; set; }

        public string Uomdesc { get; set; }

        public double? UnitRate { get; set; }

        public double? UnitPrice { get; set; }

        public double? OrigUnitPrice { get; set; }

        public bool? IsFree { get; set; }

        public double? VatValue { get; set; }

        public string Vatcode { get; set; }

        public double? Vat { get; set; }

        public int? DemandOrderQty { get; set; }

        public int? OriginalOrderQty { get; set; }

        public int? OriginalOrderBaseQty { get; set; }

        public int? OriginalOrderQtyBooked { get; set; }

        public bool? StockCheckStatus { get; set; }

        public bool? RequiredCheckStock { get; set; }

        public bool? PendingOrder { get; set; }

        public bool? WaittingStock { get; set; }

        public string AllocateType { get; set; }

        public double? OrigOrdLineAmt { get; set; }

        public double? OrigOrdLineDiscAmt { get; set; }

        public string DiscountType { get; set; }

        public double? OrigOrdLineExtendAmt { get; set; }

        public double? DisCountAmount { get; set; }

        public double? DiscountPercented { get; set; }

        public bool? IsBudget { get; set; }

        public bool? BudgetCheckStatus { get; set; }

        public string BudgetCode { get; set; }

        public string BudgetType { get; set; }

        public int? BudgetDemand { get; set; }

        public int? BudgetBooked { get; set; }

        public int? BudgetBook { get; set; }

        public bool? BudgetBookOver { get; set; }

        public string BudgetBookOption { get; set; }

        public bool? BudgetImport { get; set; }

        public int? AllowChangeSku { get; set; }

        public string PromotionOrderRule { get; set; }

        public string PromotionOrderType { get; set; }

        public string PromotionRuleofGiving { get; set; }

        public double? PromotionOrderQty { get; set; }

        public double? PromotionProductAmount { get; set; }

        public double? PromotionProductQty { get; set; }

        public string PromotionType { get; set; }

        public string PromotionCode { get; set; }

        public string PromotionDescription { get; set; }

        public string PromotionLevelCode { get; set; }

        public string PromotionLevelDescription { get; set; }

        public string RewardDescription { get; set; }

        public int? NumberPickedLevel { get; set; }

        public string OwnerType { get; set; }

        public string OwnerCode { get; set; }

        public bool? IsDeleted { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string DisPriceVolumeCode { get; set; }

        public int? DisPriceVolumeQuantities { get; set; }

        public double? DisPriceVolumeDiscount { get; set; }
    }
}
