using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;
using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models.SORecallModels
{
    #region Recall request model
    public class SORecallReqModel : SoorderRecallReq
    {
        public List<SoorderRecallReqScope> ListScope { get; set; }
        public List<SoorderRecallReqGiveBack> ListGiveBack { get; set; }
        public List<SoorderRecallReqOrder> ListOrder { get; set; }
    }

    public class SORecallReqSearch : EcoParameters 
    {
    }

    public class GetDetailRecallReqForRecallModel
    {
        public string RecallReqCode { get; set; }
        public string DistributorCode { get; set; }
    }
    public class SORecallReqListModel
    {
        public List<SoorderRecallReq> Items { get; set; }
        public MetaData MetaData { get; set; }
    }

    public class FilterRawSO : EcoParameters
    {
        public string SalesOrgCode { get; set; }
        public string TerritoryLevel { get; set; }
        public List<string> TerritoryValues { get; set; } = new List<string>();
        public List<string> DistributorCodes { get; set; } = new List<string>();
        public string ItemGroupCode { get; set; }
        public string ItemAttributeCode { get; set; }
        public string ItemAttributeLevel { get; set; }
        public string ItemCode { get; set; }
    }

    public class ListRawSO
    {
        public List<RawSOModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }

    public partial class RawSOModel
    {
        public Guid Id { get; set; }
        public string OrderRefNumber { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string Status { get; set; }
        public string SalesOrgId { get; set; }
        public string BranchId { get; set; }
        public string RegionId { get; set; }
        public string SubRegionId { get; set; }
        public string AreaId { get; set; }
        public string SubAreaId { get; set; }
        public string Dsaid { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerShiptoId { get; set; }
        public string CustomerShiptoName { get; set; }
        public string SalesRepId { get; set; }
        public string SalesRepEmpName { get; set; }
        public string InventoryId { get; set; }
        public string InventoryDescription { get; set; }
        public string ItemGroupId { get; set; }
        public int ShippedQuantities { get; set; }
        public int ShippedBaseQuantities { get; set; }
        public string Uom { get; set; }
        public string InventoryAttributeValueId1 { get; set; }
        public string InventoryAttributeValueId2 { get; set; }
        public string InventoryAttributeValueId3 { get; set; }
        public string InventoryAttributeValueId4 { get; set; }
        public string InventoryAttributeValueId5 { get; set; }
        public string InventoryAttributeValueId6 { get; set; }
        public string InventoryAttributeValueId7 { get; set; }
        public string InventoryAttributeValueId8 { get; set; }
        public string InventoryAttributeValueId9 { get; set; }
        public string InventoryAttributeValueId10 { get; set; }
        public string DistributorId { get; set; }
        public string WarehouseId { get; set; }
        public string LocationId { get; set; }
    }
    #endregion

    #region Recall model
    public class SORecallModel : SoorderRecall
    {
        public List<SoorderRecallOrder> ListOrder { get; set; }
    }

    public class SORecallSearch : EcoParameters { }

    public class SORecallListModel
    {
        public List<SoorderRecall> Items { get; set; }
        public MetaData MetaData { get; set; }
    }
    #endregion
}
