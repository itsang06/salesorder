using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class Temp_SOBudgets : AuditTable
    {
        public Guid Id { get; set; }
        public string PromotionCode { get; set; } = null!;
        public string PromotionLevel { get; set; } = null!;
        public string CustomerCode { get; set; } = null!;
        public string CustomerShiptoCode { get; set; } = null!;
        public string RouteZoneCode { get; set; }
        public string DSACode { get; set; }
        public string SubAreaCode { get; set; }
        public string AreaCode { get; set; }
        public string SubRegionCode { get; set; }
        public string RegionCode { get; set; }
        public string BranchCode { get; set; }
        public string NationwideCode { get; set; }
        public string SalesOrgCode { get; set; }
        public string BudgetCode { get; set; } = null!;
        public string ReferralCode { get; set; }
        public int BudgetBook { get; set; }
        public string BudgetType { get; set; } = null!;
        public int BudgetBooked { get; set; }
        public bool BudgetBookOver { get; set; }
        public int BudgetRemains { get; set; }
        public string CustomerBudget { get; set; } = null!;
    }
}
