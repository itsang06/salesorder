using System;

namespace ODSaleOrder.API.Infrastructure
{
    public partial class SoorderRecallOrder
    {
        public Guid Id { get; set; }

        public string RecallCode { get; set; }

        public Guid? RefDetailReqId { get; set; }

        public string CustomerCode { get; set; }

        public string CustomerName { get; set; }

        public string CustomerShiptoCode { get; set; }

        public string CustomerShiptoName { get; set; }

        public string DistributorCode { get; set; }

        public string OrderCode { get; set; }

        public DateTime? OrderDate { get; set; }

        public string ItemCode { get; set; }

        public string ItemDescription { get; set; }

        public string Uom { get; set; }

        public int RecallQty { get; set; }

        public int RecallBaseQty { get; set; }

        public string ItemGiveBackCode { get; set; }

        public string ItemGiveBackDesc { get; set; }

        public int GivBackQty { get; set; }

        public int GiveBackBaseQty { get; set; }

        public string GiveBackUom { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string CreatedBy { get; set; }

        public string UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }

        public string OwnerType { get; set; }

        public string OwnerCode { get; set; }
    }
}
