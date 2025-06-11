namespace ODSaleOrder.API.Models.ReportModel
{
    public class DisProductivityBySalesReportModel
    {
        public string InventoryAttibute { get; set; }
        public string RouteZoneID { get; set; }
        public int Qty { get; set; }
    }

    public class DisProductivityBySalesReportModelV2
    {
        public string InventoryAttibute { get; set; }
        public string RouteZoneID { get; set; }
        public int ShippedSLThung { get; set; }
        public int ShippedSLLoc { get; set; }
        public int ShippedSLChai { get; set; }
    }

    public class HoProductivityBySalesReportModel
    {
        public string InventoryAttibute { get; set; }
        public string TerritoryValueKey { get; set; }
        public int Qty { get; set; }
    }

    public class HoProductivityBySalesReportModelV2
    {
        public string InventoryAttibute { get; set; }
        public string TerritoryValueKey { get; set; }
        public int ShippedSLThung { get; set; }
        public int ShippedSLLoc { get; set; }
        public int ShippedSLChai { get; set; }
    }
}
