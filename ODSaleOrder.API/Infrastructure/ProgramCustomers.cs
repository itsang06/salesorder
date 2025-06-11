using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class ProgramCustomers
    {
        public Guid Id { get; set; }
        [MaxLength(250)] public string ProgramCustomersKey { get; set; }
        [MaxLength(250)] public string ProgramCode { get; set; }
        //Program Info
        [MaxLength(250)] public string ProgramsType { get; set; } //Promotion, Display, Accumulate
        [MaxLength(250)] public string ProgramsDescription { get; set; } //Mô tả chương trình
        [MaxLength(250)] public string ProgramsItemScope { get; set; } //Line, Group, Bundle
        [MaxLength(250)] public string ShiptoCode { get; set; }
        [MaxLength(250)] public string CustomerCode { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime EffectiveDate { get; set; } = DateTime.Now;
        public DateTime? ValidUntil { get; set; } = DateTime.Now;
        public string Shipto_Attribute1 { get; set; }
        public string Shipto_Attribute2 { get; set; }
        public string Shipto_Attribute3 { get; set; }
        public string Shipto_Attribute4 { get; set; }
        public string Shipto_Attribute5 { get; set; }
        public string Shipto_Attribute6 { get; set; }
        public string Shipto_Attribute7 { get; set; }
        public string Shipto_Attribute8 { get; set; }
        public string Shipto_Attribute9 { get; set; }
        public string Shipto_Attribute10 { get; set; }
        [MaxLength(250)] public string PromotionRefNumber { get; set; }
        public string SalesOrgCode { get; set; }// "GT2023",
        public string SicCode { get; set; }// "DRINK",
        public string RouteZoneCode { get; set; }// "RZBD007",
        public string DsaCode { get; set; }// "DSATBD01",
        public string Branch { get; set; }// "N",
        public string Region { get; set; }// "N1",
        public string SubRegion { get; set; }// null,
        public string Area { get; set; }// "N11",
        public string SubArea { get; set; }// null,
        [MaxLength(100)] public string OwnerType { get; set; }
        [MaxLength(255)] public string OwnerCode { get; set; }
    }
}
