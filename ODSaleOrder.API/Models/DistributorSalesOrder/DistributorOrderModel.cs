using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models.DistributorSalesOrder
{
    public class DistributorOrderModel
    {
        public SO_OrderInformations OrderInformations { get; set; }
        public List<SO_OrderItems> OrderItems { get; set; }
    }

    public class UpdateDistributorOrderModel
    {
        public string OrderRefNumber { get; set; }
        public string Status { get; set; }
        public string DistributorCode { get; set; }
    }

    public class FFASaleOrderModel
    {
        public FfasoOrderInformation OrderInformations { get; set; }
        public List<FfasoOrderItem> OrderItems { get; set; }
    }
}
