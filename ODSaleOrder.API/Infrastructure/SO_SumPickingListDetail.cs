using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class SO_SumPickingListDetail : AuditTable
    {
        public Guid Id { get; set; }
        [MaxLength(50)] public string SumPickingRefNumber { get; set; }
        [MaxLength(50)] public string OrderRefNumber { get; set; }
        [MaxLength(100)] public string OwnerType { get; set; }
        [MaxLength(255)] public string OwnerCode { get; set; }
    }
}
