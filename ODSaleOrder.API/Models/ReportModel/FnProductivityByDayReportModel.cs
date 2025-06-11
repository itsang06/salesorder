using System;

namespace ODSaleOrder.API.Models.ReportModel
{
    public class FnProductivityByDayReportModel
    {
        public string InventoryID { get; set; }
        public DateTime OrderDate { get; set; }
        public string ItemDescription { get; set; }
        public string SalesOrgID { get; set; }
        public string TerritoryStrID { get; set; }
        public string TerritoryValueKey { get; set; }
        public string BaseUomCode { get; set; }       
        public int ShippedBaseQuantities { get; set; }

    }
}
