using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class ItemInventoryModel
    {
        public Guid ItemId { get; set; }
        public string ItemType { get; set; }
        public string ItemGroupCode { get; set; }
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        public Guid Hierarchy { get; set; }
        public string BaseUnitCode { get; set; }
        public Guid BaseUnit { get; set; }
        public string BaseUnitDescription { get; set; }
        public string SalesUnitCode { get; set; }
        public Guid SalesUnit { get; set; }
        public string SalesUnitDescription { get; set; }
        public string PurchaseUnitCode { get; set; }
        public Guid PurchaseUnit { get; set; }
        public string PurchaseUnitDescription { get; set; }
        public bool TrackingSerialNumber { get; set; }
        public string LotSerial { get; set; }
        public string LotBatchFormat { get; set; }
        public string Attribute1 { get; set; }
        public string Attribute2 { get; set; }
        public string Attribute3 { get; set; }
        public string Attribute4 { get; set; }
        public string AttributeName1 { get; set; }
        public string AttributeName2 { get; set; }
        public string AttributeName3 { get; set; }
        public string AttributeName4 { get; set; }
        public string AttributeCode1 { get; set; }
        public string AttributeCode2 { get; set; }
        public string AttributeCode3 { get; set; }
        public string AttributeCode4 { get; set; }
        public string AttributeCode5 { get; set; }
        public string AttributeCode6 { get; set; }
        public string AttributeCode7 { get; set; }
        public string AttributeCode8 { get; set; }
        public string AttributeCode9 { get; set; }
        public string AttributeCode10 { get; set; }
        public string AttributeCodeDescription1 { get; set; }
        public string AttributeCodeDescription2 { get; set; }
        public string AttributeCodeDescription3 { get; set; }
        public string AttributeCodeDescription4 { get; set; }
        public string WareHouseCode { get; set; }
        public string LocationCode { get; set; }
        public int Available { get; set; }
        public int ConversionFactor { get; set; }
        public double AvailablePurchaseQuantity { get; set; }
        public double AvailableBaseQuantity { get; set; }
        public Guid VatId { get; set; }
        public string VatCode { get; set; }
        public double  VatValue { get; set; }

        //
        public List<Prices> Prices { get; set; }
        public List<UomConventionFactorModel> UomConventionFactor { get; set; }
    }
}
