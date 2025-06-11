using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class SO_FirstTimeCustomer : AuditTable
    {
        public Guid Id { get; set; }
        [MaxLength(50)] public string CustomerCode { get; set; }
        [MaxLength(250)] public string FullName { get; set; }
        [MaxLength(250)] public string PhoneNumber { get; set; }
        public Guid? Country { get; set; }
        public Guid? Region { get; set; }
        public Guid? State { get; set; }
        public Guid? Province { get; set; }
        public Guid? District { get; set; }
        public Guid? Wards { get; set; }
        public Guid? City { get; set; }
        public string BusinessAddress { get; set; } //text
        [MaxLength(250)] public string StreetLine { get; set; }
        [MaxLength(250)] public string DeptNo { get; set; }
        [MaxLength(250)] public string DistributorShiptoID { get; set; }
        [MaxLength(50)] public string DistributorCode { get; set; }
        [MaxLength(100)] public string OwnerType { get; set; }
        [MaxLength(255)] public string OwnerCode { get; set; }
    }
}
