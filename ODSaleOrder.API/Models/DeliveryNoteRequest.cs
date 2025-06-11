using System.Collections.Generic;

namespace ODSaleOrder.API.Models
{
    public class DeliveryNoteRequest
    {
        public string DistributorCode { get; set; }
        public List<string> OrderRefNumbers { get; set; }
    }
}
