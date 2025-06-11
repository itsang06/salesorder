using System;
using System.ComponentModel.DataAnnotations;

namespace ODSaleOrder.API.Infrastructure.SOInfrastructure
{
    public class Principal
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; }
        [MaxLength(150)]
        public string Email { get; set; }
        [MaxLength(200)]
        public string Web { get; set; }
        [MaxLength(20)]
        public string Phone { get; set; }
        public string Fax { get; set; }
        [MaxLength(200)]
        public string Address1 { get; set; }
        [MaxLength(200)]
        public string Address2 { get; set; }
        [MaxLength(30)]
        public string Country { get; set; }
        //[MaxLength(30)]
        //public string State { get; set; }
        /// <summary>
        /// This Code used as 1s subdomain for Principal link. Ex: [Code].1solution.link
        /// </summary>
        [MaxLength(5)]
        public string Code { get; set; }
        [MaxLength(255)]
        public string SecretKey { get; set; }
        /// <summary>
        /// [Code].1solution.link
        /// </summary>
        //[MaxLength(200)]
        //public string EcoWeb { get; set; }
        [MaxLength(500)]
        public string Description { get; set; }
        ///// <summary>
        ///// If this info null will use with format: DBName: ecosystem[Code], User: postgres[Code], Pass: PAssword123[Code]
        ///// </summary>
        //[MaxLength(500)]
        //public string ConnectionInfo { get; set; }
        public Guid PackageId { get; set; }
        public bool IsActive { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        [MaxLength(256)]
        public string CreatedBy { get; set; }
        [MaxLength(256)]
        public string UpdatedBy { get; set; }
        public string DynamicFieldValue { get; set; }
        [MaxLength(50)]
        public string InitializationStatus { get; set; }
        public bool? IsODSystem { get; set; }
        public string LinkODSystem { get; set; }
    }
}
