using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class Temp_ProgramsDetails
    {
        public Guid Id { get; set; }
        [MaxLength(250)] public string ProgramDetailsKey { get; set; }
        [MaxLength(250)] public string ProgramsCode { get; set; }
        [MaxLength(250)] public string Level { get; set; }
        [MaxLength(250)] public string Description { get; set; }
        [MaxLength(250)] public string Type { get; set; }  // According box/carton, According Pass Level
        public int RequiredQuantities { get; set; }
        public decimal RequiredAmount { get; set; }
        public decimal ProportionByQuantity { get; set; } = 0;
        public bool IsDeleted { get; set; }
        // public int MinQty { get; set; } // Nếu loại khuyến mãi là Group thì đây là giá trị tối thiếu phải đạt được
        // public decimal MinAmt { get; set; } // Nếu loại khuyến mãi là Group thì đây là giá trị tối thiếu phải đạt được
    }
}
