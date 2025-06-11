using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class ProgramCustomerDetailsItems
    {
        public Guid Id { get; set; }
        [MaxLength(250)] public string ProgramCustomersDetailCode { get; set; }
        [MaxLength(250)] public string ProgramCustomerItemsGroupCode { get; set; }
        [MaxLength(250)] public string PromotionRefNumber { get; set; }
        [MaxLength(250)] public string Description { get; set; }
        [MaxLength(250)] public string InventoryId { get; set; } //InvenId, Bình thường là = ItemCode , nếu là record khuyến mãi thì sẽ có Mã riêng
        [MaxLength(250)] public string ItemCode { get; set; }
        public Guid? ItemId { get; set; }
        [MaxLength(250)] public string ItemDescription { get; set; }
        [MaxLength(250)] public string UOMCode { get; set; }
        public bool IsDisCountLine { get; set; } //Cờ để xác định đây là record khuyến mãi hay k
        [MaxLength(250)] public string DiscountLineCode { get; set; }
        public int OrderQuantites { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public double DiscountPercented { get; set; }
        public decimal DisCountAmount { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPromotion { get; set; } = true;
        [MaxLength(250)] public string PromotionType { get; set; }
        public Guid? VatId { get; set; }
        public decimal VatValue { get; set; } = 0;
        [MaxLength(250)] public string VATCode { get; set; }
        public int BaseOrderQuantities { get; set; }
        [MaxLength(250)] public string PromotionCode { get; set; }
        [MaxLength(250)] public string ItemShortName { get; set; }
        public Guid? BaseUnit { get; set; } = Guid.Empty;
        public Guid? SalesUnit { get; set; } = Guid.Empty;
        public Guid? PurchaseUnit { get; set; } = Guid.Empty;
        public int OriginalQty { get; set; } = 0;
        public decimal OriginalAmt { get; set; } = 0;
        public string ProgramDetailDesc { get; set; }
        [MaxLength(100)] public string OwnerType { get; set; }
        [MaxLength(255)] public string OwnerCode { get; set; }
    }
}
