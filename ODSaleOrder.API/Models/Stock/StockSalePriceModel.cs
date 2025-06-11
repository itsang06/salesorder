using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class Prices
    {
        public string Uom { get; set; }
        public string UomDesc { get; set; }
        public double Price { get; set; }
    }

    public class StockSalePriceModel
    {
        public string ItemGroupCode { get; set; }
        public List<Prices> Prices { get; set; }
    }

}
