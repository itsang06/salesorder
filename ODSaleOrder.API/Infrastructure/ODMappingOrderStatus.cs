using System;
using System.ComponentModel.DataAnnotations;

namespace ODSaleOrder.API.Infrastructure
{
    public partial class ODMappingOrderStatus
    {
        public Guid Id { get; set; }

        public string SaleOrderStatus { get; set; }

        public string OneShopOrderStatus { get; set; }

        public bool? IsDeleted { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string ImportStatus { get; set; }

        public string OwnerType { get; set; }

        public string OwnerCode { get; set; }
        [MaxLength(255)]
        public string SaleOrderStatusName { get; set; }
        [MaxLength(255)]
        public string OneShopOrderStatusName { get; set; }

    }
}
