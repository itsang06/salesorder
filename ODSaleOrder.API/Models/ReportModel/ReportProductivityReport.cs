namespace ODSaleOrder.API.Models.ReportModel
{
    public class ReportProductivityReport
    {
        public string RouteZoneID { get; set; }
        public string InventoryID { get; set; }
        public string ItemDescription { get; set; }
        public int SLThungOrder { get; set; }
        public int SLLocOrder { get; set; }
        public int SLChaiOrder { get; set; }
        public int SLThungShipped { get; set; }
        public int SLLocShipped { get; set; }
        public int SLChaiShipped { get; set; }
    }
}
