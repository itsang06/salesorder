using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class Temp_ProgramDetailsItemsGroup
    {
        public Guid Id { get; set; }
        [MaxLength(250)] public string ProgramDetailsItemsGroupKey { get; set; }
        [MaxLength(250)] public string Description { get; set; }
        [MaxLength(250)] public string ProgramDetailsKey { get; set; }
        [MaxLength(250)] public string ItemGroupCode { get; set; }
        public Guid ItemGroupId { get; set; }
        [MaxLength(250)] public string UOMCode { get; set; }
        public int FixedQuantities { get; set; } //Số lượng có sẵn trong gói bundle
        public bool IsDeleted { get; set; }
        // public bool IsRequired { get; set; } // Nếu loại khuyến mãi là Group thì ItemGroup có cờ này để phân biệt đâu là Itemgroup bắt buộc phải mua
        public int MinQty { get; set; } // Nếu loại khuyến mãi là Group thì đây là giá trị tối thiếu phải đạt được
        public decimal MinAmt { get; set; } // Nếu loại khuyến mãi là Group thì đây là giá trị tối thiếu phải đạt được
    }
}
