namespace ODSaleOrder.API.Models.ReportModel
{
    public class FnReportShippingStatusModel
    {
        public string DistributorCode { get; set; }
        public string InventoryID { get; set; }
        public string ItemDescription { get; set; }
        public string UOM { get; set; }
        public int ActualShippedQTY { get; set; }
        public int OrderQTY { get; set; }
    }
}
