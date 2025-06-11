using System;

namespace ODSaleOrder.API.Infrastructure.SOInfrastructure
{
    public partial class FfasoOrderInformation
    {
        public Guid Id { get; set; }
        public string OrderRefNumber { get; set; }
        public string VisitID { get; set; }
        public string External_OrdNBR { get; set; }
        public bool NotInSubRoute { get; set; }
        public bool IsDirect { get; set; }
        public string OrderType { get; set; }
        public string PeriodID { get; set; }
        public string WareHouseID { get; set; }
        public string WareHouseDescription { get; set; }
        public string PrincipalID { get; set; }
        public string DistributorCode { get; set; }
        public DateTime? VisitDate { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? ExpectShippedDate { get; set; }
        public string Status { get; set; }
        public string SalesOrgID { get; set; }
        public string TerritoryStrID { get; set; }
        public string TerritoryValueKey { get; set; }
        public string AreaId { get; set; }
        public string BranchId { get; set; }
        public string RegionId { get; set; }
        public string SubAreaId { get; set; }
        public string SubRegionId { get; set; }
        public string DSAID { get; set; }
        public string NSD_ID { get; set; }
        public string Branch_Manager_ID { get; set; }
        public string Region_Manager_ID { get; set; }
        public string Sub_Region_Manager_ID { get; set; }
        public string Area_Manager_ID { get; set; }
        public string Sub_Area_Manager_ID { get; set; }
        public string DSA_Manager_ID { get; set; }
        public string RZ_Suppervisor_ID { get; set; }
        public string SIC_ID { get; set; }
        public string SalesRepID { get; set; }
        public string SalesRepName { get; set; }
        public string SalesRepPhone { get; set; }
        public string RouteZoneID { get; set; }
        public string RouteZOneType { get; set; }
        public string RouteZonelocation { get; set; }
        public string Created_By { get; set; }
        public string Owner_ID { get; set; }
        public string CustomerId { get; set; }
        public string CustomerShiptoID { get; set; }
        public string CustomerShiptoName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerPhone { get; set; }
        public string Shipto_Attribute1 { get; set; }
        public string Shipto_Attribute2 { get; set; }
        public string Shipto_Attribute3 { get; set; }
        public string Shipto_Attribute4 { get; set; }
        public string Shipto_Attribute5 { get; set; }
        public string Shipto_Attribute6 { get; set; }
        public string Shipto_Attribute7 { get; set; }
        public string Shipto_Attribute8 { get; set; }
        public string Shipto_Attribute9 { get; set; }
        public string Shipto_Attribute10 { get; set; }
        public string Source { get; set; }
        public int? Orig_Ord_SKUs { get; set; }
        public int? Orig_Ord_Qty { get; set; }
        public int? Orig_Promotion_Qty { get; set; }
        public double? Orig_Ord_Amt { get; set; }
        public double? Promotion_Amt { get; set; }
        public double? Orig_Ord_Disc_Amt { get; set; }
        public double? Orig_Ordline_Disc_Amt { get; set; }
        public double? Orig_Ord_Extend_Amt { get; set; }
        public string DiscountID { get; set; }
        public string DiscountDescription { get; set; }
        public string DiscountType { get; set; }
        public DateTime? ExpectDeliveryNote { get; set; }
        public int? TotalLine { get; set; }
        public string OrderDescription { get; set; }
        public string Note { get; set; }
        public string DeliveryTimeType { get; set; }
        public string DeliveryTimeTypeDesc { get; set; }
        public DateTime? DeliveryTime { get; set; }
        public string DeliveryMethod { get; set; }
        public string DeliveryMethodDesc { get; set; }
        public string PaymentType { get; set; }
        public string PaymentTypeDesc { get; set; }
        public bool? WaittingBudget { get; set; }
        public bool? WaittingStock { get; set; }
        public bool? AllowRemoveFreeItem { get; set; }
        public bool? IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string ImportStatus { get; set; }
        public string ReasonCode { get; set; }
        public bool? IsSplitOrder { get; set; }

        //Enhance Calculate Tax
        public double? Orig_Ord_TotalBeforeTax_Amt { get; set; }
        public double? Orig_Ord_TotalAfterTax_Amt { get; set; }
        public double? Orig_Ord_TotalTax_Amt { get; set; }

        //Enhance Cancel Order FFA
        public string ReasonCancel { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}
