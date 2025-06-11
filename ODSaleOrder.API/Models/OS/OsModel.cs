using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using Sys.Common.Models;
using System.Collections.Generic;

namespace ODSaleOrder.API.Models.OS
{
    public class OsOrderModel : OsOrderInformation
    {
        public List<OsOrderItem> OrderItems { get; set; }
    }

    public class ListOsOrderModel
    {
        public int TotalOrder { get; set; }
        public int TotalOrderFailed { get; set; }
        public List<OsOrderModel> Items { get; set; }
        public MetaData MetaData { get; set; }
    }
    public class SearchOsOrderModel : EcoparamsWithGenericFilter
    {
        public List<string> FilterStatus { get; set; }
    }

    public class ImportListOSOrder
    {
        public List<string> ExternalOrdNbrs { get; set; } = new List<string>();
    }

    public class SA_UserWithDistributorShiptoView
    {
        public string UserCode { get; set; }
        public string DistributorCode { get; set; }
        public string DistributorName { get; set; }
        public string DistributorShiptoCode { get; set; }
        public string DistributorShiptoName { get; set; }
    }

    public class DisShipto
    {
        public string ShiptoCode { get; set; }
        public string ShiptoName { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class ExGetInfoCusAndShioptoByOutletCodeModel
    {
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerShiptoCode { get; set; }
        public string CustomerShiptoName { get; set; }
        public string CustomerShiptoAttribute1 { get; set; }
        public string CustomerShiptoAttribute2 { get; set; }
        public string CustomerShiptoAttribute3 { get; set; }
        public string CustomerShiptoAttribute4 { get; set; }
        public string CustomerShiptoAttribute5 { get; set; }
        public string CustomerShiptoAttribute6 { get; set; }
        public string CustomerShiptoAttribute7 { get; set; }
        public string CustomerShiptoAttribute8 { get; set; }
        public string CustomerShiptoAttribute9 { get; set; }
        public string CustomerShiptoAttribute10 { get; set; }
    }

    public partial class ExCreateCustomer
    {
        public string DistributorName { get; set; }

        public string CustomerId { get; set; }

        public string CustomerName { get; set; }

        public string CustomerAddress { get; set; }

        public string CustomerPhone { get; set; }

        public string CusAddressProvinceId { get; set; }

        public string CusAddressCountryId { get; set; }

        public string CusAddressDistrictId { get; set; }

        public string CusAddressWardId { get; set; }

        public string CusAddressStreetNo { get; set; }

        public double? CusAddressLat { get; set; }

        public double? CusAddressLong { get; set; }
    }
}
