using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class AuditTable
    {
        public bool IsDeleted { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        [MaxLength(250)] public string CreatedBy { get; set; }
        [MaxLength(250)] public string UpdatedBy { get; set; }
    }
}
