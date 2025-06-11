using System;
using System.Collections.Generic;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;

namespace ODSaleOrder.API.Models
{
    public class ItemMng_IventoryItemModel
    {
        public ItemMng_InventoryItem InventoryItem { get; set; }
        public List<UomConversionModel> UomConversion { get; set; }
    }
    public class ItemMng_InventoryItem
    {
        public Guid Id { get; set; }
        public string InventoryItemId { get; set; }
        public string Status { get; set; }
        public string ShortName { get; set; }
        public string ReportName { get; set; }
        public string Description { get; set; }
        public string Erpcode { get; set; }
        public string DistribiutorCode { get; set; }
        public bool IsStock { get; set; }
        public string ItemType { get; set; }
        public bool OrderItem { get; set; }
        public bool PurchaseItem { get; set; }
        public bool Lsnumber { get; set; }
        public bool Competitor { get; set; }
        public Guid Vat { get; set; }
        public Guid BaseUnit { get; set; }
        public Guid SalesUnit { get; set; }
        public Guid PurchaseUnit { get; set; }
        public Guid Attribute1 { get; set; }
        public Guid Attribute2 { get; set; }
        public Guid Attribute3 { get; set; }
        public Guid Attribute4 { get; set; }
        public Guid Attribute5 { get; set; }
        public Guid Attribute6 { get; set; }
        public Guid Attribute7 { get; set; }
        public Guid Attribute8 { get; set; }
        public Guid Attribute9 { get; set; }
        public Guid Attribute10 { get; set; }
        public string Note { get; set; }
        public int DelFlg { get; set; }
        public string Avatar { get; set; }
        public string GroupId { get; set; }
        public decimal Point { get; set; }
        public Guid Hierarchy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }


    public class ItemMng_InventoryItemAttribute
    {
        public Guid Id { get; set; }
        public Guid ItemID { get; set; }
        public string ItemCode { get; set; }
        public string ItemERPCode { get; set; }
        public string BaseUOMCode { get; set; }
        public string SalesUOMCode { get; set; }
        public string PurchaseUOMCode { get; set; }
        public string VATCode { get; set; }
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
    }

    // public class UomConversionModel
    // {
    //     public Guid Id { get; set; }
    //     public Guid? FromUnit { get; set; }
    //     public string FromUnitName { get; set; }
    //     public Guid? ToUnit { get; set; }
    //     public string ToUnitName { get; set; }
    //     public int? ConversionFactor { get; set; }
    //     public int? DM { get; set; }
    //     public string MultiplyDivide { get; set; }
    //     public string MultiplyDivideName { get; set; }
    // }


        public class ItemBookedModel
    {
        public string ItemCode { get; set; }
        public string Uom { get; set; }
        public int Quantity { get; set; }
        public string BaseUom { get; set; }
        public int BaseQty { get; set; }
    }

    public class BookingStockReqModel
    {
        
        public string LocationCode { get; set; }
        public string DistributorCode { get; set; }
        public string DistributorShiptoCode { get; set; }
        public string AllocateType { get; set; }
        public string ItemCode { get; set; }
        public string Uom { get; set; }
        public int OrderQuantities { get; set; }
        public string BaseUom { get; set; }
        public string TransactionId { get; set; }
        public string VisitId { get; set; }
        public bool AllocationStock { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public int OrderBaseQuantities { get; set; } = 0;

        [System.Text.Json.Serialization.JsonIgnore]
        public bool ForceConversion { get; set; } = false;

        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime RequestDate { get; set; } = DateTime.Now;
        [System.Text.Json.Serialization.JsonIgnore]
        public Guid RequestId { get; set; } = Guid.Empty;

    }

    public class ExInventoryItemModel
    {
        public Guid Id { get; set; }
        public string InventoryItemId { get; set; }
        public string Status { get; set; }
        public string ShortName { get; set; }
        public string ReportName { get; set; }
        public string Description { get; set; }
        public string ERPCode { get; set; }
        public bool LSNumber { get; set; }
        public Guid Vat { get; set; }
        public string VATCode { get; set; }
        public Guid Hierarchy { get; set; }
        public string ItemGroupCode { get; set; }
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
        public string BaseUOMCode { get; set; }
        public string SalesUOMCode { get; set; }
        public string PurchaseUOMCode { get; set; }
        public Guid BaseUnit { get; set; }
        public Guid SalesUnit { get; set; }
        public Guid PurchaseUnit { get; set; }
        public List<UomConversionModel> UomConversion { get; set; } = new List<UomConversionModel> { };
    }
}
