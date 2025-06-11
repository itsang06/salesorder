namespace ODSaleOrder.API.Models
{
    public class QuantitiesForOrderPromotion
    {
        public string ItemCode { get; set; }

        public string UomType { get; set; }

        public string PromotionUom { get; set; }

        public decimal Quantity { get; set; }

        public string BaseUom { get; set; }

        public int BaseQuantity { get; set; }

        public decimal ConversionFactor { get; set; }
    }
}
