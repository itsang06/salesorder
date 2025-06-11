using System;
namespace ODSaleOrder.API.Models
{
    public class ProductivityByDayReportModel
    {
        public Guid Id { get; set; }
        public string RouteZoneID { get; set; }
        public Guid OrderItemId { get; set; } = Guid.Empty;
        public string InventoryID { get; set; }
        public string ItemDescription { get; set; }
        public Guid? BaseUnit { get; set; } = Guid.Empty;
        public int OrderBaseQuantities { get; set; } = 0;
        public int ShippedBaseQuantities { get; set; } = 0; // quy đổi số lượng base cho ShippedQuantities
        public DateTime? OrderDate { get; set; }
        public bool IsFree { get; set; }
    }
}