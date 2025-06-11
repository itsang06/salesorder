using System;
using System.Collections.Generic;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;

namespace ODSaleOrder.API.Models
{
    #region UOM
    public class UomModel
    {
        public Guid Id { get; set; }
        public string UomId { get; set; }
        public string Description { get; set; }
        public DateTime? EffectiveDateFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsUse { get; set; }
    }

    public class UomConversionModel
    {
        public Guid Id { get; set; }
        public Guid FromUnit { get; set; }
        public string FromUnitName { get; set; }
        public Guid ToUnit { get; set; }
        public string ToUnitName { get; set; }
        public decimal ConversionFactor { get; set; }
        public int DM { get; set; }
        public string MultiplyDivide { get; set; }
        public string MultiplyDivideName { get; set; }
    }
    #endregion

    public class VATDetail
    {
        public Guid Id { get; set; }  // "300f9968-5287-4701-8de6-db2f0e89994c",
        public string VatId { get; set; }  // "VAT02",
        public string Description { get; set; }  // "VAT 5 %",
        public decimal VatValues { get; set; } = 0;  // 5,
        public DateTime EffectiveDateFrom { get; set; }  // "2021-10-20T17:00:00",
        public DateTime? ValidUntil { get; set; }  // null,
        public string IsUse { get; set; }  // false
    }
}
