using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class ProgramCustomersDetail
    {
        public Guid Id { get; set; }
        [MaxLength(250)] public string ProgramCustomersDetailCode { get; set; }
        [MaxLength(250)] public string ProgramCustomersKey { get; set; }
        [MaxLength(250)] public string ProgramDetailsKey { get; set; }
        [MaxLength(250)] public string PromotionRefNumber { get; set; }

        public int ActualQantities { get; set; }
        public decimal ActualAmount { get; set; }

        public decimal RemainAmount { get; set; }
        public int RemainQuantities { get; set; }
        public int SuggestQantities { get; set; }

        // ## Data detail Khuyến mãi
        [MaxLength(250)] public string DetailLevel { get; set; }
        [MaxLength(250)] public string DetailDescription { get; set; }
        //Data tượng trưng cho khuyến mãi
        [MaxLength(250)] public string DetailType { get; set; } // According box/carton, According Pass Level
        public int DetailQuantities { get; set; }
        public decimal DetailAmount { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        // ##
        public bool IsDeleted { get; set; }

        public string ProgramsBuyType { get; set; } // Quantity, Amount, 
        public string ProgramsGivingType { get; set; } // FreeItem, Amount, Percented

        public string ProductTypeForSale { get; set; }
        public string ProductTypeForGift { get; set; }
        public string ItemHierarchyLevelForSale { get; set; } //: "IT03",
        public string ItemHierarchyLevelForGift { get; set; } //: "IT03",
        public int QuantityPurchased { get; set; }
        public int OnEachQuantity { get; set; }
        public decimal ValuePurchased { get; set; }
        public decimal OnEachValue { get; set; }
        public string BudgetCode { get; set; }
        public string BudgetType { get; set; }
        public string BudgetAllocationLevel { get; set; }
        public float BudgetBook {get;set;}
        public float BudgetBooked {get;set;}
        public bool BudgetBookOver {get;set;}
        public bool Allowance {get;set;}
        [MaxLength(100)] public string OwnerType { get; set; }
        [MaxLength(255)] public string OwnerCode { get; set; }
    }
}
