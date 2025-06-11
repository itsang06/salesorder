namespace ODSaleOrder.API.Models
{
    public class InvoiceHeaderModel
    {
        public string OrderRefNumber { get; set; }
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
        public string CustomerId { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerBankAccount { get; set; }
        public string CustomerBankName { get; set; }
        public string CustomerTaxCode { get; set; }
        public string SalemanCode { get; set; }
        public string SalemanName { get; set; }

    }
}
