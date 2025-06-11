using System;
using System.Collections.Generic;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;

namespace ODSaleOrder.API.Models
{
    #region ItemGroup

    public class ItemGroupDetailModel
    {
        public ItemGroupModel ItemGroupModel { get; set; }
        public List<InventoryModel> ListInventory { get; set; }
    }

    public class ItemGroupModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
    }


    public class InventoryModel
    {
        public Guid Id { get; set; }
        public string InventoryItemId { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
        public string ItemType { get; set; }
        public string Status { get; set; }
        public bool IsChecked { get; set; }
        public Guid BaseUnit { get; set; }
        public Guid SalesUnit { get; set; }
        public Guid PurchaseUnit { get; set; }
        public List<UomConversionModel> UomConversion { get; set; }
    }
    #endregion

    public class ItemGroupFromHierarchyResult
    {
        public string ItemgroupCode { get; set; } // "0173q1l1t1",
        public string Description { get; set; } // "0173q1l1t1",
        public string IT01Code { get; set; } // "D1",
        public string IT01Name { get; set; } // "Beverage",
        public string IT02Code { get; set; } // "C1",
        public string IT02Name { get; set; } // "Non-Alcoholic",
        public string IT03Code { get; set; } // "C1",
        public string IT03Name { get; set; } // "Carbonated",
        public string IT04Code { get; set; } // "B1",
        public string IT04Name { get; set; } // "Energy Drink",
        public string IT05Code { get; set; } // "S1",
        public string IT05Name { get; set; } // "Number 1",
        public string IT06Code { get; set; } // "q1",
        public string IT06Name { get; set; } // "24 chai 1 thùng",
        public string IT07Code { get; set; } // "l1",
        public string IT07Name { get; set; } // "RGB",
        public string IT08Code { get; set; } // "t1",
        public string IT08Name { get; set; } // "240 ml",
        public string IT09Code { get; set; } // null,
        public string IT09Name { get; set; } // null,
        public string IT10Code { get; set; } // "1",
        public string IT10Name { get; set; } // "Cola"
    }
}
