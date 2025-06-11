using System;

namespace ODSaleOrder.API.Models.ReportModel
{
    public class FnReportTrackingOrderModel
    {
        public string DistributorCode { get; set; }
        public string OrderRefNumber { get; set; }
        public string Status { get; set; }
        public string SalesRepID { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public int TotalBaseQty { get; set; }
        public decimal Amount { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
}
