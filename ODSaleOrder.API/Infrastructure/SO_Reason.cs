using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class SO_Reason : AuditTable
    {
        public Guid Id { get; set; }
        [MaxLength(50)] public string ReasonCode { get; set; }
        [MaxLength(50)] public string Value { get; set; }
        [MaxLength(250)] public string Description { get; set; }
        public bool Used { get; set; }
        [MaxLength(100)] public string OwnerType { get; set; }
        [MaxLength(255)] public string OwnerCode { get; set; }
    }
}
