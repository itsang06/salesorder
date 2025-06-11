using System.Collections.Generic;

namespace ODSaleOrder.API.Models.DistributorSalesOrder
{
    public class DistributorCancelOrderModel
    {
        public string SaleOrgCode { get; set; }
        public string SicCode { get; set; }
        public string CustomerCode { get; set; }
        public string ShiptoCode { get; set; }
        public string RouteZoneCode { get; set; }
        public string DsaCode { get; set; }
        public string Branch { get; set; }
        public string Region { get; set; }
        public string SubRegion { get; set; }
        public string Area { get; set; }
        public string SubArea { get; set; }
        public string DistributorCode { get; set; }
        public string DistributorShiptoCode { get; set; }
        public string OrderRefNumber { get; set; }
        public string ReasonCode { get; set; }
        public string ReasonName { get; set; }
        public List<AppliedPromotionModel> AppliedPromotionList { get; set; }

    }

    public class AppliedPromotionModel
    {
        public string PromotionCode { get; set; }
        public string LevelId { get; set; }
        public string BudgetCode { get; set; }
        public int BudgetQuantity { get; set; }
    }
    public class AppliedPromotionResultModel : AppliedPromotionModel
    {
        public string ErrorMessage { get; set; }
    }
}
