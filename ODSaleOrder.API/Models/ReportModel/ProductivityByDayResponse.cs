using System;

namespace ODSaleOrder.API.Models.ReportModel
{
    public class ProductivityByDayResponse
    {
        public string InventoryID { get; set; }
        public DateTime OrderDate { get; set; }
        public string ItemDescription { get; set; }
        public string SalesOrgID { get; set; }
        public string TerritoryStrID { get; set; }
        public string TerritoryValueKey { get; set; }
        public string BaseUomCode { get; set; }
        public string RouteZoneID { get; set; }
        public int QuantityThung { get; set; }
        public int QuantityLoc { get; set; }
        public int QuantityChai { get; set; }
    }
}
