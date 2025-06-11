using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class UomConventionFactorModel
    {
        public string ItemCode { get; set; }
        //public string ItemDescription { get; set; }
        public string FromUnit { get; set; }
        public string ToUnit { get; set; }
        public decimal ConversionFactor { get; set; }
    }

}
