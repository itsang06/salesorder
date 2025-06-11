using System.ComponentModel.DataAnnotations.Schema;

namespace ODSaleOrder.API.Models.Distributor
{
    public class DistributorCommonInfoModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class DistributorBasicInfoModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        [NotMapped]
        public string? RouteZoneCode { get; set; }
    }

    public class DistributorListRequestModel
    {
        public string ManagerCode { get; set; }
        public string SaleOrgCode { get; set; }
    }
}
