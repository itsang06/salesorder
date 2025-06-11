using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ODSaleOrder.API.Models.SaleHistories
{
    public class SaleVolumnReportModel
    {
        public string EmployeeCode { get; set; }
        public string RouteZoneId { get; set; }
        public string InventoryName { get; set; }
        public decimal? TotalVolumn { get; set; }
        public string Uom { get; set; }
        public string UomName { get; set; }
        public bool? IsFreeProduct { get; set; }
    }
}
