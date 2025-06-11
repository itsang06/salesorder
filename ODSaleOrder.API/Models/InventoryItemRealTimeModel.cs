using RDOS.INVAPI.Infratructure;
using Sys.Common.Models;
using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class ResultInventoryItemRealTimeModel
    {
        public List<INV_AllocationDetailModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }
    public class RequestInventoryItemRealTimeModel : EcoParameters
    {
        public string DistributorCode { get; set; }
        public string WareHouseCode { get; set; }
        public string ItemCode { get; set; }
    }

    public class UpdateBookedAllocationModel
    {
        public string ItemCode { get; set; }
        public string WareHouseCode { get; set; }
        public string DistributorCode { get; set; }
        public string LocationCode { get; set; }
        public int SOBooked { get; set; }
        public Guid FFAOrderId { get; set; } = Guid.NewGuid();
        public Guid OneShopId { get; set; } = Guid.NewGuid();
    }

    public class QueryAllocationModel
    {
        public string DistributorCode { get; set; }
        public string WarehouseCode { get; set; }
        public string LocationCode { get; set; }
        public string ItemCode { get; set; }
    }

    public class BookAllocationModel
    {
        public Guid OrderID { get; set; } // Guid order
        public string OneShopID { get; set; }
        public string FFAVisitID { get; set; }
        public string CreatedBy { get; set; }
        public int BookBaseQty { get; set; }
        public int BookQty { get; set; }
        public string BookUom { get; set; }
        public string ItemGroupCode { get; set; }
        public int Priority { get; set; }
        public string OrderLineId { get; set; }
        public string OrderType { get; set; }
    }
}
