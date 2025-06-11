using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

#nullable disable

namespace ODSaleOrder.API.Infrastructure
{
    public partial class SO_SumPickingListHeader : AuditTable
    {
        public Guid Id { get; set; }
        [MaxLength(50)] public string SumPickingRefNumber { get; set; }
        [MaxLength(50)] public string DistributorCode { get; set; }
        // public string DistributorShiptoID { get; set; }
        public DateTime? TransactionDate { get; set; }
        [MaxLength(250)] public string Status { get; set; }
        [MaxLength(250)] public string Vehicle { get; set; }
        [MaxLength(50)] public string DriverCode { get; set; }
        [MaxLength(50)] public string WareHouseID { get; set; }
        [MaxLength(250)] public string NumberPlates { get; set; }
        [MaxLength(250)] public string VehicleLoad { get; set; }
        [MaxLength(250)] public string TotalWeight { get; set; }
        public bool IsPrinted { get; set; }
        public int PrintedCount { get; set; }
        public DateTime? LastedPrintDate { get; set; }
        public int TotalOrderQuantities { get; set; }
        public int TotalOriginOrderQuantities { get; set; }
        public int TotalShippedQuantities { get; set; }
        public int TotalFailedQuantities { get; set; }
        public int TotalShippingQuantities { get; set; }
        public int TotalRemainQuantities { get; set; }
        [MaxLength(100)] public string OwnerType { get; set; }
        [MaxLength(255)] public string OwnerCode { get; set; }
    }
}
