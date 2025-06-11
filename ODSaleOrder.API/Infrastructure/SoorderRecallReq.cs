using System;

namespace ODSaleOrder.API.Infrastructure
{
    public partial class SoorderRecallReq
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = null!;

        public string ExternalCode { get; set; }

        public string Reason { get; set; }

        public DateTime? OrderDateFrom { get; set; }

        public DateTime? OrderDateTo { get; set; }

        public DateTime? RecallDateFrom { get; set; }

        public DateTime? RecallDateTo { get; set; }

        public string FilePath { get; set; }

        public string FileName { get; set; }

        public string Status { get; set; }

        public string RecallProductType { get; set; }

        public string RecallProductLevel { get; set; }

        public string RecallProductCode { get; set; }

        public string RecallProductDescription { get; set; }

        public string GiveBackProductType { get; set; }

        public string GiveBackProductLevel { get; set; }

        public bool SameRecallItem { get; set; }

        public string SaleOrgCode { get; set; }

        public string TerritoryStructureCode { get; set; }

        public string ScopeType { get; set; }

        public string SaleTerritoryLevel { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string CreatedBy { get; set; }

        public string UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }

        public string OwnerType { get; set; }

        public string OwnerCode { get; set; }

        public bool IsSync { get; set; }
    }
}
