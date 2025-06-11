using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class ProgramCustomerItemsGroup
    {
        public Guid Id { get; set; }
        [MaxLength(250)] public string ProgramCustomerItemsGroupCode { get; set; }
        [MaxLength(250)] public string ProgramCustomersDetailCode { get; set; }
        [MaxLength(250)] public string PromotionRefNumber { get; set; }
        public int Quantities { get; set; }
        public int ItemGroupQuantities { get; set; }
        public decimal Amount { get; set; }
        //Data 
        [MaxLength(250)] public string Description { get; set; }
        [MaxLength(250)] public string ItemGroupCode { get; set; }
        [MaxLength(250)] public string UOMCode { get; set; }
        public int FixedQuantities { get; set; } //Số lượng có sẵn trong gói bundle
        public bool IsDeleted { get; set; }
        public int MinQty { get; set; } // Nếu loại khuyến mãi là Group thì đây là giá trị tối thiếu phải đạt được
        public decimal MinAmt { get; set; } // Nếu loại khuyến mãi là Group thì đây là giá trị tối thiếu phải đạt được
        public string ProductTypeForSale { get; set; }
        public string InventoryItemCode { get; set; }
        public string ItemHierarchyValueForSale { get; set; }
        [MaxLength(100)] public string OwnerType { get; set; }
        [MaxLength(255)] public string OwnerCode { get; set; }
    }
}
