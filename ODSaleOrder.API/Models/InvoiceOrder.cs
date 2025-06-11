using System;

namespace ODSaleOrder.API.Models
{
    public class InvoiceOrder
    {
        public Guid Id { get; set; }
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
        public double Vat { get; set; }
        public bool IsPrinted { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

    }
}
