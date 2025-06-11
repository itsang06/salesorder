namespace ODSaleOrder.API.Models.ReportModel
{
    public class FnProductivityReportModel
    {
        public string InventoryID { get; set; }
        public string ItemDescription { get; set; }
        public string BaseUomCode { get; set; }
        public string SalesOrgID { get; set; }
        public string TerritoryStrID { get; set; }
        public string TerritoryValueKey { get; set; }
        public int OrderBaseQuantities { get; set; }
        public int ShippedBaseQuantities { get; set; }

    }
}
