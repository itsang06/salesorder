namespace ODSaleOrder.API.Models.Distributor
{
    public class DistributorRouteZoneModel
    {
        public string RouteZoneCode { get; set; }
        public string RouteZoneDesc { get; set; }
        public string RouteZoneType { get; set; }
        public string RouteZoneLocation { get; set; }
        public string RouteZoneSaleTypeCode { get; set; }
        public string RouteZoneSaleMethod { get; set; }
        public string DsaCode { get; set; }
        public string DistributorCode { get; set; }
        public string PrincipalCode { get; set; }
    }

    public class DisRouteZoneBasicModel
    {
        public string RouteZoneCode { get; set; }
        public string RouteZoneDesc { get; set; }
    }

    public class DisRouteZoneBasicReqModel
    {
        public string CustomerCode { get; set; }
        public string ShiptoCode { get; set; }
        public string DsaCode { get; set; }
    }
}
