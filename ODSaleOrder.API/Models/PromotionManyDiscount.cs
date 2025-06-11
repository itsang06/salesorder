namespace ODSaleOrder.API.Models
{
    public class PromotionManyDiscount
    {
        public string PromotionId { get; set; }
        public decimal? LevelOrderQuantity { get; set; }
        public decimal? LevelOrderAmount { get; set; }
        public string LevelId { get; set; }
    }
}
