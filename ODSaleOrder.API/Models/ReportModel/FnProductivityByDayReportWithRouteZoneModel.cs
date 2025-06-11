using System;

namespace ODSaleOrder.API.Models.ReportModel
{
    public class FnProductivityByDayReportWithRouteZoneModel
    {
        public string InventoryID { get; set; }
        public DateTime OrderDate { get; set; }
        public string RouteZoneID { get; set; }
        public string ItemDescription { get; set; }
        public string BaseUomCode { get; set; }
        public int ShippedBaseQuantities { get; set; }

    }
}
