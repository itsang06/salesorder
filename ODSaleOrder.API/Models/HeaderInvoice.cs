using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class HeaderInvoice
    {
        public string OrderRefNumber { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string InvoiceFormCode { get; set; }
        public string InvoiceSignCode { get; set; }
        public string InvoiceReferenceNumber { get; set; }
        public string DistributorCode { get; set; }
        public string DistributorName { get; set; }
        public string DistributorAddress { get; set; }
        public string DistributorTaxCode { get; set; }
        public string DistributorPhone { get; set; }
        public string DistributorFax { get; set; }
        public string DistributorBankAccount { get; set; }
        public string DistributorBankName { get; set; }
        public string DistributorShiptoCode { get; set; }
        public string DistributorShiptoName { get; set; }
        public string DistributorShiptoAddress { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerBankAccount { get; set; }
        public string CustomerBankName { get; set; }
        public string CustomerTaxCode { get; set; }
        public string SalemanCode { get; set; }
        public string SalemanName { get; set; }
        public decimal Vat { get; set; }
        public bool IsPrinted { get; set; }
        public string ShortName { get; set; }
        public string PaymentType { get; set; }
        public string PaymentTypeDesc { get; set; }
    }

    public class BodyInvoice
    {
        public string OrderRefNumber { get; set; }
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        public string Uom { get; set; }
        public string UomDesc { get; set; }
        public int OrderQuantities { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal VatAmount { get; set; }
        public decimal VatPercent { get; set; }
        public string PromotionDescription { get; set; }
        public decimal DiscountAmount { get; set; }
        public bool IsFree { get; set; }
    }

    public class DetailInvoice
    {
        public HeaderInvoice Header { get; set; }
        public List<BodyInvoice> Body { get; set; }
    }
}
