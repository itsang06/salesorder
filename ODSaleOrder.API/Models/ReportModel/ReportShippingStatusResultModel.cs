namespace ODSaleOrder.API.Models.ReportModel
{
    public class ReportShippingStatusResultModel
    {
        public string DistributorCode { get; set; }
        public string InventoryID { get; set; }
        public string ItemDescription { get; set; }
        public int SlthungOrder { get; set; }
        public int SllocOrder { get; set; }
        public int SlchaiOrder { get; set; }
        public int SlthungShipped { get; set; }
        public int SllocShipped { get; set; }
        public int SlchaiShipped { get; set; }
    }
}
