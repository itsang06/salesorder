using System;
using System.ComponentModel.DataAnnotations;
using ODSaleOrder.API.Infrastructure;

namespace RDOS.INVAPI.Infratructure
{
    public partial class INV_AllocationDetail : AuditTable
    {
        [Key]
        public Guid Id { get; set; }
        [MaxLength(50)] public string ItemKey { get; set; }
        public Guid ItemId { get; set; }
        [MaxLength(50)] public string ItemCode { get; set; }
        [MaxLength(50)] public string BaseUom { get; set; }
        [MaxLength(250)] public string ItemDescription { get; set; }
        [MaxLength(50)] public string WareHouseCode { get; set; }
        [MaxLength(50)] public string LocationCode { get; set; }
        [MaxLength(50)] public string DistributorCode { get; set; }
        public int OnHand { get; set; }
        public int OnSoShipping { get; set; }
        public int OnSoBooked { get; set; }
        public int Available { get; set; }
        [MaxLength(50)] public string Attribute1 { get; set; }
        [MaxLength(50)] public string Attribute2 { get; set; }
        [MaxLength(50)] public string Attribute3 { get; set; }
        [MaxLength(50)] public string Attribute4 { get; set; }
        [MaxLength(50)] public string Attribute5 { get; set; }
        [MaxLength(50)] public string Attribute6 { get; set; }
        [MaxLength(50)] public string Attribute7 { get; set; }
        [MaxLength(50)] public string Attribute8 { get; set; }
        [MaxLength(50)] public string Attribute9 { get; set; }
        [MaxLength(50)] public string Attribute10 { get; set; }
        [MaxLength(50)] public string ItemGroupCode { get; set; }
        [MaxLength(50)] public string DSACode { get; set; }
        [MaxLength(250)] public string ShortName { get; set; }
        public Guid? Hierarchy { get; set; }
        public bool IsDeleted { get; set; } = false;
        [MaxLength(50)] public bool LSNumber { get; set; } = true;
        [MaxLength(250)] public string ReportName { get; set; }
        [MaxLength(100)] public string OwnerType { get; set; }
        [MaxLength(255)] public string OwnerCode { get; set; }
    }
}
