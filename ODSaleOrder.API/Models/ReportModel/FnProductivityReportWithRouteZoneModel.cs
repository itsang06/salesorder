namespace ODSaleOrder.API.Models.ReportModel
{
    public class FnProductivityReportWithRouteZoneModel
    {
        public string InventoryID { get; set; }
        public string ItemDescription { get; set; }
        public string RouteZoneID { get; set; }
        public string BaseUomCode { get; set; }
        public int OrderBaseQuantities { get; set; }
        public int ShippedBaseQuantities { get; set; }

    }
}
