using ODSaleOrder.API.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System;

namespace ODSaleOrder.API.Models.PrincipalModel
{
    public class ODDistributorSchema : AuditTable
    {
        [Key]
        public Guid Id { get; set; }
        public string SchemaName { get; set; }
        public string DistributorCode { get; set; }
    }
}
