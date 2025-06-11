using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ODSaleOrder.API.Infrastructure
{
    public partial class OsorderStatusHistory
    {
        public Guid Id { get; set; }
        public string OrderRefNumber { get; set; }
        public string ExternalOrdNbr { get; set; }
        public DateTime? OrderDate { get; set; }
        public string OutletCode { get; set; }
        public string DistributorCode { get; set; }
        public string OneShopStatus { get; set; }
        public string OneShopStatusName { get; set; }
        public string Sostatus { get; set; }
        public string SOStatusName { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
