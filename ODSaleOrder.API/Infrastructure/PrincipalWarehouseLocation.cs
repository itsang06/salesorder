using System.ComponentModel.DataAnnotations;
using System;

namespace ODSaleOrder.API.Infrastructure
{
    public class PrincipalWarehouseLocation
    {
        [Key]
        public Guid Id { get; set; }
        public long Code { get; set; }
        [MaxLength(255)]
        public string Decscription { get; set; }
        public bool IsDefault { get; set; } = false;
        public bool AllowIn { get; set; } = false;
        public bool AllowOut { get; set; } = false;
        public bool AllowPromotion { get; set; } = false;
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string DeletedBy { get; set; }
        public DateTime? DeletedDate { get; set; }
    }
}
