using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class ApplyPromotionByOrderList
    {
        public string LevelId { get; set; }
        public string LevelDesc { get; set; }
        public string PromotionId { get; set; }
        public string PromotionName { get; set; }
        public string PromotionType { get; set; }
        public string OrderRule { get; set; }
        public string ProductType { get; set; }
        public string OrderBy { get; set; }
        public string PackingUomId { get; set; }
        public string PackingUomName { get; set; }
        public int LevelOrderQty { get; set; }
        public decimal? LevelOrderAmount { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string AttId { get; set; }
        public string AttName { get; set; }
        public string AttCode { get; set; }
        public string AttCodeName { get; set; }
        public int? OrderProductQty { get; set; }
        public bool? IsGiftProduct { get; set; }
        public string FreeItemType { get; set; }
        public string FreeAmountType { get; set; }
        public string RuleOfGiving { get; set; }
        public decimal? FreeAmount { get; set; }
        public decimal? FreePercentAmount { get; set; }
        public int? NumberOfFreeItem { get; set; }
        public bool? IsDefaultProduct { get; set; }
        public bool? AllowExchange { get; set; }
        public decimal? ExchangeRate { get; set; }
        public bool IsFreeProduct { get; set; }
        public bool IsSalesProduct { get; set; }
        public string SicId { get; set; }
        public int? LevelFreeQty { get; set; }
        public decimal OnEach { get; set; }
        public string DiscountType { get; set; }
        public int? RequiredMinQty { get; set; }
        public decimal? MinValue { get; set; }
        public bool FreeSameProduct { get; set; }
        public bool IsApplyBudget { get; set; }
        public string BudgetValueCode { get; set; }
        public string BudgetQtyCode { get; set; }
        public bool IsBudgetQtyBookOver { get; set; }
        public bool? IsBudgetValueBookOver { get; set; }
        public string BaseUnit { get; set; }
        public int ConversionFactor { get; set; }

        public int BudgetQuantity { get; set; } // So Suat
        public int Quantity { get; set; } // So luong san pham ban dc huong KM

        public List<GiftPromotion> Gifts { get; set; }
        public int? LevelTotalFreeQuantity { get; set; }
        public decimal? LevelTotalFreeAmount { get; set; }
        public string UomType { get; set; }
        public bool SkuFullCase { get; set; }
    }


    

    public class ApplyPromotionResponse
    {
        public List<ApplyPromotionByOrderList> AppliedPromotions { get; set; }
        public List<ProductItem> NotApplyPromotions { get; set; }
    }

    public class BudgetRequest
    {
        public string BudgetCode { get; set; }
        public string PromotionId { get; set; }
        public string LevelId { get; set; }
        public int BudgetQuantity { get; set; } = 0;
    }


}
