using System;
using System.Collections.Generic;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;

namespace ODSaleOrder.API.Models
{
    public class INV_TransactionModel
    {
        public string OrderCode { get; set; }
        public string Description { get; set; }
        public Guid ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        public string Uom { get; set; }
        public int Quantity { get; set; }
        public int BaseQuantity { get; set; }
        public int OrderBaseQuantity { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string WareHouseCode { get; set; }
        public string LocationCode { get; set; }
        public string DistributorCode { get; set; }
        public string DSACode { get; set; }
        public string ReasonCode { get; set; }
        public string ReasonDescription { get; set; }
    }


    public class AllocationDetailParameter
    {
        public string ItemCode { get; set; }
        public string DistributorCode { get; set; }
        public string WareHouseCode { get; set; }
        public string LocationCode { get; set; }
    }

    public class AvailableAllocationItemQuery : AllocationDetailParameter
    {
        public int BaseQuantities { get; set; }
    }

    public partial class INV_AllocationDetailModel : AuditTable
    {
        public Guid Id { get; set; }
        public string ItemKey { get; set; }
        public Guid ItemId { get; set; }
        public string ItemCode { get; set; }
        public string BaseUom { get; set; }
        public string ItemDescription { get; set; }
        public string WareHouseCode { get; set; }
        public string LocationCode { get; set; }
        public string DistributorCode { get; set; }
        public int OnHand { get; set; }
        public int OnSoShipping { get; set; }
        public int OnSoBooked { get; set; }
        public int Available { get; set; }
        public Guid? Atrribute1 { get; set; }
        public Guid? Atrribute2 { get; set; }
        public Guid? Atrribute3 { get; set; }
        public Guid? Atrribute4 { get; set; }
        public Guid? Atrribute5 { get; set; }
        public Guid? Atrribute6 { get; set; }
        public Guid? Atrribute7 { get; set; }
        public Guid? Atrribute8 { get; set; }
        public Guid? Atrribute9 { get; set; }
        public Guid? Atrribute10 { get; set; }
        public string ItemGroupCode { get; set; }
        public string DSACode { get; set; }
        public string ShortName { get; set; }
        public Guid? Hierarchy { get; set; }
        public bool IsDeleted { get; set; } = false;

        public bool IsDefault {get;set;} = false;
    }

    public class SearchAllocationItemWithDistributorModel : EcoParameters
    {
        public string DistributorCode { get; set; }
        public string WareHouseCode { get; set; } = "";
        public string LocationCode { get; set; } = "";
    }

    public class BookingAdjustmentQty
    {
        public int AdjustedQty { get; set; }
        public int AdjustedBaseQty { get; set; }
    }
}
