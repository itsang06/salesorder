using System;

namespace ODSaleOrder.API.Infrastructure
{
    public partial class SoorderRecallReqScope
    {
        public Guid Id { get; set; }

        public string RecallReqCode { get; set; }

        public string Code { get; set; }

        public string Description { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string CreatedBy { get; set; }

        public string UpdatedBy { get; set; }

        public bool? IsDeleted { get; set; }

        public string OwnerType { get; set; }

        public string OwnerCode { get; set; }
    }
}
