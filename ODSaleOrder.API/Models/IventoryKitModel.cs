using System;
using System.Collections.Generic;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;

namespace ODSaleOrder.API.Models
{
    public class KitModel
    {
        public string Id { get; set; }
        public string ItemKitId { get; set; }
        public string Status { get; set; }
        public bool IsNonStock { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
        public string Avatar { get; set; }
        public Guid? Vat { get; set; }
        public int Point { get; set; }
        public bool OrderItem { get; set; }
        public bool PurchaseItem { get; set; }
        public bool Competitor { get; set; }
        public bool LSNumber { get; set; }
        public Guid BaseUnit { get; set; }
        public Guid SalesUnit { get; set; }
        public Guid PurchaseUnit { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsChecked { get; set; }
        public bool IsUse { get; set; }
        public List<UomConversionModel> uomConversionModels { get; set; }
        public List<InventoryItemConversionModel> inventoryItemConversionModels { get; set; }
    }



    public class InventoryItemConversionModel
    {
        public Guid Id { get; set; }
        public bool IsStock { get; set; }
        public Guid InventoryItemIDDb { get; set; }
        public string InventoryItemID { get; set; }
        public string InventoryItemDescription { get; set; }
        public int Quantity { get; set; }
        public Guid Uom { get; set; }
        public string UomName { get; set; }
    }


}
