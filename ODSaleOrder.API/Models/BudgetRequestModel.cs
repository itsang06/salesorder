namespace ODSaleOrder.API.Models
{
    public class BudgetRequestModel
    {
        public string budgetCode { get; set;}
        public string budgetType { get; set;}
        public string customerCode { get; set;}
        public string customerShipTo { get; set;}
        public string saleOrg { get; set;}
        public string budgetAllocationLevel { get; set;}
        public int budgetBook { get; set;}
        public string salesTerritoryValueCode { get; set;}
        public string promotionCode { get; set;}
        public string promotionLevel { get; set;}
        public string routeZoneCode { get; set;}
        public string dsaCode { get; set;}
        public string subAreaCode { get; set;}
        public string areaCode { get; set;}
        public string subRegionCode { get; set;}
        public string regionCode { get; set;}
        public string branchCode { get; set;}
        public string nationwideCode { get; set;}
        public string salesOrgCode { get; set;}
        public string referalCode { get; set;}
        public string distributorCode { get; set; }
    }
}
