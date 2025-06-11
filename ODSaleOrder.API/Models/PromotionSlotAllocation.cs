using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class PromotionSlotAllocation
    {
        public int SlotIndex { get; set; }
        public List<ProductUsed> RequiredUsed { get; set; }
        public List<ProductUsed> OtherUsed { get; set; }
        public List<ProductUsed> Remaining { get; set; }
    }

    public class ProductUsed
    {
        public string ProductCode { get; set; }
        public decimal UsedQuantity { get; set; }
        public string Role { get; set; } // "Required" or "Other"
    }

}
