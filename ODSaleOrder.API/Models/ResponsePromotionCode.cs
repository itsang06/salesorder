namespace ODSaleOrder.API.Models
{
    public class ResponsePromotionCode
    {
        public string PromomotionId { get; set; }
        public string PromotionName { get; set; }
        public string LevelId { get; set; }
        public string LevelDesc { get; set; }

        public decimal LevelOrderAmount { get; set; }
        public decimal TotalFreePercentAmount { get; set; }
        public decimal TotalFreeAmount { get; set; }
    }
}
