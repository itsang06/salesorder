using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class PromotionGroupResponse
    {
        public string PromotionId { get; set; }
        public string PromotionName { get; set; }
        public string PromotionType { get; set; }
        public List<ApplyPromotion> AppliedPromotions { get; set; }
        public List<NotApplyPromotion> NotApplyPromotions { get; set; }

    }
    public class ApplyPromotion
    {
        public string LevelId { get; set; }
        public string LevelDesc { get; set; }
        public int? LevelOrderQty { get; set; }
        public decimal? LevelOrderAmount { get; set; }
        public bool? IsApplyBudget { get; set; }
        public string BudgetValueCode { get; set; }
        public string BudgetQtyCode { get; set; }
        public decimal? BudgetQuantity { get; set; }
        public int? Quantity { get; set; }
        public string OrderBy { get; set; }
        public decimal? LevelTotalFreeQuantity { get; set; }
        public decimal? LevelTotalFreeAmount { get; set; }
        public List<SalesProduct> SalesProducts { get; set; }
        public List<GiftPromotion> Gifts { get; set; }
    }

    public class SalesProduct
    {
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string ProductType { get; set; }
        public string PackingUomId { get; set; }
        public string PackingUomName { get; set; }
        public string AttName { get; set; }
        public string AttCode { get; set; }
        public string AttCodeName { get; set; }
        public bool? RequiredMinQty { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? Price { get; set; }
        public decimal? Quantity { get; set; }
        public string Uom { get; set; }
        public decimal? BaseQuantity { get; set; }
        public string BaseUnit { get; set; }
        public int? ConversionFactor { get; set; }
    }

    public class NotApplyPromotion
    {
        public string ItemGroupCode { get; set; }
        public string ItemCode { get; set; }
        public decimal? Quantity { get; set; }
        public string Uom { get; set; }
        public decimal? Price { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? BaseQuantity { get; set; }
        public string BaseUom { get; set; }
        public int? ConversionFactor { get; set; }
    }

}
