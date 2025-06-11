namespace ODSaleOrder.API.Models.ReportModel
{
    public class FnRouteZoneProductivityByProductReportModel
    {
        public string RouteZoneID { get; set; }
        public string InventoryAttibute { get; set; }
        public int Qty { get; set; }
    }
    public class FnRouteZoneProductivityByProductReportModelV2
    {
        public string RouteZoneID { get; set; }
        public string InventoryAttibute { get; set; }
        public int ShippedSLThung { get; set; }
        public int ShippedSLLoc { get; set; }
        public int ShippedSLChai { get; set; }
    }
}
