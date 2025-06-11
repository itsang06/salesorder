namespace ODSaleOrder.API.Models.ReportModel
{
    public class FnSaslesDetailReportModel
    {
        public string DistributorCode { get; set; }
        public double TotalAmount { get; set; }
        public double VAT { get; set; }
        public double DiscountAmount { get; set; }
        public double Revenue { get; set; }
    }
}
