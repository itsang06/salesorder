using System;
using System.Collections.Generic;
using ODSaleOrder.API.Infrastructure;
using Sys.Common.Models;

namespace ODSaleOrder.API.Models
{
    #region Program
    public class StandardItemModel
    {
        public Guid ItemGroupId { get; set; }
        public string ItemGroupCode { get; set; }
        public Guid InventoryId { get; set; }
        public string InventoryCode { get; set; }
        public string InventoryDescription { get; set; }
        public int? StdRuleCode { get; set; }
        public string StdRuleName { get; set; }
        public int? Priority { get; set; }
        public DateTime? EffectiveDateFrom { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool AllocateToItemGroup { get; set; }
        public int? Ratio { get; set; }
        public int? OnHand { get; set; }
        public int Avaiable { get; set; }
    }


    #endregion
}
