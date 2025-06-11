using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class Temp_ProgramDetailReward
    {
        public Guid Id { get; set; }
        [MaxLength(250)] public string ProgramDetailRewardCode { get; set; }
        [MaxLength(250)] public string ProgramDetailsKey { get; set; }
        [MaxLength(250)] public string Description { get; set; }
        [MaxLength(250)] public string Type { get; set; } // loại thưởng nào: ITEM, AMOUNT, PERCENTED
        [MaxLength(250)] public string ItemCode { get; set; }
        public Guid? ItemId { get; set; }
        [MaxLength(250)] public string UOMCode { get; set; }
        public int Quantities { get; set; }
        public decimal Amount { get; set; }
        public double DiscountPercented { get; set; }
        public bool IsDeleted { get; set; }
        public int BaseQuantities { get; set; }
        public string BaseUomCode { get; set; }
        
    }
}
