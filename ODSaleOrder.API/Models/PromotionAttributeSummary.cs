namespace ODSaleOrder.API.Models
{
    public class PromotionAttributeSummary
    {
        public string LevelId { get; set; }
        public string LevelDesc { get; set; }
        public string PromotionId { get; set; }
        public string PromotionName { get; set; }
        public string PromotionType { get; set; }
        public string OrderRule { get; set; }
        public int? LevelOrderQty { get; set; }
        public decimal? LevelOrderAmount { get; set; }
        public string AttCode { get; set; }
        public string AttCodeName { get; set; }
        public string OrderBy { get; set; }
        public bool IsApplyBudget { get; set; } = false;
        public string BudgetQtyCode { get; set; }
        public string BudgetValueCode { get; set; }
        public decimal? MinValue { get; set; }
        public int? ConversionFactor { get; set; }
        public string PackingUomId { get; set; }
        public string BaseUnit { get; set; }
        public string RuleType { get; set; } // RequireMin / NoRequireMin
        public string ProductCodes { get; set; } // CSV: "130000280,130000299"
        public int? TotalRequiredBaseUnit { get; set; }
        public bool SkuFullCase { get; set; } = true;
        public string ProductInfo { get; set; }
    }

}
