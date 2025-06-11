using System;

namespace ODSaleOrder.API.Models
{
    public class OrderInvoiceModel
    {
        public long OrderRefNumber { get; set; }
        public DateTime? OrderDate { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string DistributorCode { get; set; }
        public string DistributorName { get; set; }
        public string OwnerCode { get; set; }
        public string OwnerName { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string InvoiceFormCode { get; set; }
        public string InvoiceSignCode { get; set; }
        public string InvoiceReferenceNumber { get; set; }
    }
}
