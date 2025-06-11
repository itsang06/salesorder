using System;

namespace ODSaleOrder.API.Infrastructure.SOInfrastructure
{
    public partial class FfasoImportItem
    {
        public Guid Id { get; set; }
        public string External_OrdNBR { get; set; }
        public string ItemCode { get; set; }
        public string ItemGroupId { get; set; }
        public string LocationID { get; set; }
        public string UOM { get; set; }
        public int QtyNeedBook { get; set; }
        public int QtyBooked { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }
}
