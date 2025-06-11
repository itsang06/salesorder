using System;

namespace ODSaleOrder.API.Infrastructure
{
    public partial class SoorderRecallReqGiveBack
    {
        public Guid Id { get; set; }

        public string RecallReqCode { get; set; }

        public string ItemCode { get; set; }

        public string ItemDescription { get; set; }

        public string ItemGroupCode { get; set; }

        public string ItemGroupDescription { get; set; }

        public string ItemAttributeCode { get; set; }

        public string ItemAttributeDescription { get; set; }

        public string Uom { get; set; }

        public int? Quantity { get; set; }

        public bool IsDefault { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string CreatedBy { get; set; }

        public string UpdatedBy { get; set; }

        public bool? IsDeleted { get; set; }

        public string OwnerType { get; set; }

        public string OwnerCode { get; set; }
    }
}
