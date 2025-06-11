using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ODSaleOrder.API.Infrastructure;

namespace RDOS.INVAPI.Infratructure
{
    public partial class INV_InventoryTransaction : AuditTable
    {
        [Key]
        public Guid Id { get; set; }
        [MaxLength(50)] public Guid ItemId { get; set; }
        [MaxLength(50)] public string ItemCode { get; set; }
        [MaxLength(250)] public string ItemDescription { get; set; }
        [MaxLength(50)] public string Uom { get; set; }
        public int Quantity { get; set; }
        public int BaseQuantity { get; set; }
        public int? OrderBaseQuantity { get; set; }
        public DateTime TransactionDate { get; set; }
        [MaxLength(50)] public string TransactionType { get; set; }
        [MaxLength(50)] public string WareHouseCode { get; set; }
        [MaxLength(50)] public string LocationCode { get; set; }
        [MaxLength(50)] public string DistributorCode { get; set; }
        [MaxLength(50)] public string OrderCode { get; set; }
        [MaxLength(255)] public string Description { get; set; }
        [MaxLength(50)] public string ItemKey { get; set; }  // FK_AllocationDetail
        [MaxLength(50)] public string ReasonCode { get; set; }
        [MaxLength(250)] public string ReasonDescription { get; set; }
        public int? BegQty { get; set; } = 0;
        public int? Receipt { get; set; } = 0;
        public int? Issue { get; set; } = 0;
        public int? EndQty { get; set; } = 0;
        public bool IsDeleted { get; set; } = false;

        [MaxLength(100)] public string FFAVisitId { get; set; }
        [MaxLength(100)] public string OneShopId { get; set; }
        [MaxLength(50)] public string ItemGroupCode { get; set; }
        public int Priority { get; set; }

        [NotMapped]
        public bool IsCreateOrderItem { get; set; } = false;

        [NotMapped]
        public bool IsCreateInFlow { get; set; } = false;
        [MaxLength(100)] public string OwnerType { get; set; }
        [MaxLength(255)] public string OwnerCode { get; set; }
        [MaxLength(100)] public string Source { get; set; }
        [MaxLength(100)] public string TransactionId { get; set; }
        [MaxLength(255)] public string OrderLineId { get; set; }
        [MaxLength(255)] public string OrderType { get; set; }
    }
}
