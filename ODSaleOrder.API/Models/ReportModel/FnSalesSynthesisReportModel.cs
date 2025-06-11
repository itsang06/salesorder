using System;

namespace ODSaleOrder.API.Models.ReportModel
{
    public class FnSalesSynthesisReportModel
    {
        public string DistributorCode { get; set; }
        public string RouteZoneID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Amount { get; set; }
    }
}
