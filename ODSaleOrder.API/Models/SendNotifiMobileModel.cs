using System;
using System.Collections.Generic;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;

namespace ODSaleOrder.API.Models
{
    public class SendNotifiMobileModel
    {
        public string EmployeeCode { get; set; }
        public string ExternalNumber { get; set; }
        public string CustomerName { get; set; }
    }
}
