using System;
using System.ComponentModel.DataAnnotations;

namespace ODSaleOrder.API.Infrastructure.SOInfrastructure
{
    public class SO_SalesOrderSetting
    {
        public Guid Id { get; set; }
        public string OrderRefNumber { get; set; }
        public int LeadDate { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        [MaxLength(100)] public string OwnerType { get; set; }
        [MaxLength(255)] public string OwnerCode { get; set; }
        public int DeliveryLeadDate { get; set; }
    }
}
