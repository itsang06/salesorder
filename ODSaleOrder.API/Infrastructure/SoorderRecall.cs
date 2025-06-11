using System;

namespace ODSaleOrder.API.Infrastructure
{
    public partial class SoorderRecall
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = null!;

        public string RequestRecallReason { get; set; }

        public string RequestRecallCode { get; set; }

        public string RecallType { get; set; }

        public string Description { get; set; }

        public string DistributorShiptoCode { get; set; }

        public string RecallLocationCode { get; set; }

        public string GiveBackLocationCode { get; set; }

        public string Status { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string CreatedBy { get; set; }

        public string UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }

        public string OwnerType { get; set; }

        public string OwnerCode { get; set; }
    }
}
