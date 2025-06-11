using System;
namespace ODSaleOrder.API.Models.SaleHistories
{
    public class OrderResultModel
    {
        public string EmployeeCode { get; set; }
        public string Key { get; set; }
        public DateTime VisitDate { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public string Actual { get; set; }
        public string Target { get; set; }
        public string Type { get; set; }
        public string Uom { get; set; }
    }
}

