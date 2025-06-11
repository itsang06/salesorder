using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class ProductWithStockModel
    {
        public string TerritoryStructureCode { get; set; }
        public string AttributeValue { get; set; }
        public Guid? DSAId { get; set; }
        public string DSACode { get; set; }
        //public List<string> ItemGroupCode { get; set; }
        public string DistributorCode { get; set; }

        public List<OutletAttributes> OutletAttributes { get; set; }
    }

    public class OutletAttributes
    {
        public string AttributeLevel { get; set; }
        public string AttributeValue { get; set; }
    }
}
