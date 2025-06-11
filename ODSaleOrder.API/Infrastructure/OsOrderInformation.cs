using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace ODSaleOrder.API.Infrastructure
{
    public partial class OsOrderInformation
    {
        public Guid Id { get; set; }

        public string OrderRefNumber { get; set; }

        public string ExternalOrdNbr { get; set; }

        public bool? IsDirect { get; set; }

        public string OrderType { get; set; }

        public string PrincipalId { get; set; }

        public string DistributorCode { get; set; }

        public DateTime? OrderDate { get; set; }

        public string CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }

        public string CustomerPhone { get; set; }

        public string Source { get; set; }

        public int? OrigOrdSkus { get; set; }

        public int? OrigOrdQty { get; set; }

        public int? OrigPromotionQty { get; set; }

        public double? OrigOrdAmt { get; set; }

        public double? PromotionAmt { get; set; }

        public double? OrigOrdDiscAmt { get; set; }

        public double? OrigOrdlineDiscAmt { get; set; }

        public double? OrigOrdExtendAmt { get; set; }

        public string DiscountId { get; set; }

        public string DiscountDescription { get; set; }

        public string DiscountType { get; set; }

        public int? TotalLine { get; set; }

        public DateTime? ShippedDate { get; set; }

        public DateTime? ExpectShippedDate { get; set; }

        public string OrderDescription { get; set; }

        public string PaymentType { get; set; }

        public string PaymentTypeDesc { get; set; }

        public string ImportStatus { get; set; }

        public string Status { get; set; }

        public string CustomerType { get; set; }

        public string MainCustomerId { get; set; }

        public string PaymentStatus { get; set; }

        public string PaymentBankNote { get; set; }

        public string DisBankName { get; set; }

        public string DisBankAccount { get; set; }

        public string DisBankAccountName { get; set; }

        public string DeliveryAddress { get; set; }

        public string DeliveryAddressNote { get; set; }

        public string ReceiverName { get; set; }

        public string ReceiverPhone { get; set; }

        public bool? IsDeleted { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? CreatedDate { get; set; }

        public string UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string CusAddressProvinceId { get; set; }

        public string CusAddressProvince { get; set; }

        public string CusAddressCountryId { get; set; }

        public string CusAddressCountry { get; set; }

        public string CusAddressDistrictId { get; set; }

        public string CusAddressDistrict { get; set; }

        public string CusAddressWardId { get; set; }

        public string CusAddressWard { get; set; }

        public string CusAddressStreetNo { get; set; }

        public double? CusAddressLat { get; set; }

        public double? CusAddressLong { get; set; }

        public string DeliveryAddressProvinceId { get; set; }

        public string DeliveryAddressProvince { get; set; }

        public string DeliveryAddressCountryId { get; set; }

        public string DeliveryAddressCountry { get; set; }

        public string DeliveryAddressDistrictId { get; set; }

        public string DeliveryAddressDistrict { get; set; }

        public string DeliveryAddressWardId { get; set; }

        public string DeliveryAddressWard { get; set; }

        public string DeliveryAddressStreetNo { get; set; }

        public double? DeliveryAddressLat { get; set; }

        public double? DeliveryAddressLong { get; set; }

        public string SOStatus { get; set; }
    }
}
