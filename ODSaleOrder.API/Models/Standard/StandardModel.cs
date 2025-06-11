using System;
using System.Collections.Generic;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;

namespace ODSaleOrder.API.Models
{
    public class StandardRequestModel
    {
        public string DistributorCode { get; set; }
        public string DistributorShiptoCode { get; set; }
        public string ItemGroupCode { get; set; }
        public int Quantity { get; set; }
    }
}
