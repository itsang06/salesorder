namespace ODSaleOrder.API.Models.ReportModel
{
    public class FnDsaProductivityByProductReportModel
    {
        public string DSAID { get; set; }
        public string InventoryAttibute { get; set; }
        public int Qty { get; set; }
    }

    public class FnDsaProductivityByProductReportModelV2
    {
        public string DSAID { get; set; }
        public string InventoryAttibute { get; set; }
        public int ShippedSLThung { get; set; }
        public int ShippedSLLoc { get; set; }
        public int ShippedSLChai { get; set; }
    }
}
