using System;

namespace ODSaleOrder.API.Models
{
    public class SalesPeriodResultModel
    {
        public Guid MyProperty { get; set; }
        public string Type { get; set; }
        public string Code { get; set; }
        public int Ordinal { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
