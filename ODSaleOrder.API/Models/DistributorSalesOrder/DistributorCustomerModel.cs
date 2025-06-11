using Sys.Common.Models;
using System;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models.DistributorSalesOrder
{
    public class DistributorCustomerModel
    {
        public Guid CustomerId { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string CustomerType { get; set; }
        public bool IsFirstTimeCustomer { get; set; }
        public int TotalCount { get; set; }
    }

    public class ListDistributorCustomerModel
    {
        public List<DistributorCustomerModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }


}
