using System.Collections.Generic;
using System;

namespace ODSaleOrder.API.Models
{
    public class GetDiscountByCustomer
    {
        public int ObjectId { get; set; }
        public Guid ObjectGuidId { get; set; }
        public int Code { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
        public Data Data { get; set; }

    }

    public class Data
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string ShortName { get; set; }
        public string FullName { get; set; }
        public string Scheme { get; set; }
        public string Status { get; set; }
        public string StatusDescription { get; set; }
        public DateTime EffectiveDateFrom { get; set; }
        public DateTime ValidUntil { get; set; }
        public string SaleOrg { get; set; }
        public string ScopeType { get; set; }
        public string SicCode { get; set; }
        public string ApplicableObjectType { get; set; }
        public int DiscountType { get; set; }
        public string OwnerType { get; set; }
        public string OwnerCode { get; set; }
        public string SchemaName { get; set; }
    }

}
