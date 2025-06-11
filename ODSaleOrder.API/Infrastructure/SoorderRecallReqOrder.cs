using System;

namespace ODSaleOrder.API.Infrastructure
{
    public partial class SoorderRecallReqOrder
    {
        public Guid Id { get; set; }

        public string RecallReqCode { get; set; }

        public string CustomerCode { get; set; }

        public string CustomerName { get; set; }

        public string CustomerShiptoCode { get; set; }

        public string CustomerShiptoName { get; set; }

        public string DistributorCode { get; set; }

        public string WarehouseId { get; set; }

        public string LocationId { get; set; }

        public string SalesRepId { get; set; }

        public string SalesRepEmpName { get; set; }

        public string Status { get; set; }

        public string OrderCode { get; set; }

        public DateTime? OrderDate { get; set; }

        public string ItemCode { get; set; }

        public string ItemDescription { get; set; }

        public string Uom { get; set; }

        public int? OrderQuantity { get; set; }

        public int? OrderBaseQuantity { get; set; }

        public bool? IsRecall { get; set; }

        public string RecallCode { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string CreatedBy { get; set; }

        public string UpdatedBy { get; set; }

        public bool? IsDeleted { get; set; }

        public string OwnerType { get; set; }

        public string OwnerCode { get; set; }
    }
}
