using System;
namespace ODSaleOrder.API.Models.SaleHistories
{
    public class SaleResultRequest
    {
        public string EmployeeCode { get; set; }

        public string VisitDate { get; set; }
    }


    public class SaleVolumnReportRequest
    {
        public string EmployeeCode { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
    }

    public class SyncTransactionRequest
    {
        public string EmployeeCode { get; set; }
        public string DistributorCode { get; set; }
        public int? Period { get; set; }
        public string ConditionColumn { get; set; }

    }
}

