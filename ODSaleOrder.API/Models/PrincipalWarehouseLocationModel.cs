using Sys.Common.Models;
using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class PrincipalWarehouseLocationModel
    {
        public Guid Id { get; set; }
        public int Code { get; set; }
        public string LocationCode { get; set; }
        public bool IsDefault { get; set; }
        public bool AllowIn { get; set; }
        public bool AllowOut { get; set; }
        public bool AllowPromotion { get; set; }
        public string Decscription { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
        public string CodeDescription { get; set; }
    }

    public class PrincipalWarehouseLocationListFilter
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? Code { get; set; }
        public string Name { get; set; }
    }

    public class PrincipalWarehouseLocationListModel
    {
        public List<PrincipalWarehouseLocationModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }
}
