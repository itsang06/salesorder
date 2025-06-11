using ODSaleOrder.API.Models.Customer;
using ODSaleOrder.API.Models.Distributor;
using Sys.Common.Models;
using System;

namespace ODSaleOrder.API.Services.Distributor
{
    public interface IDistributorService
    {
        BaseResultModel GetBasicInfo(string DistributorCode);
        BaseResultModel GetCustomerList(string DistributorCode);
        BaseResultModel GetRoutZoneList(string DistributorCode);
        BaseResultModel GetSalemanList(string DistributorCode);
        BaseResultModel GetShipperList(string DistributorCode);
        BaseResultModel GetCustomerListWithPaging(SearchCustomerModel input, string DistributorCode);
        BaseResultModel GetShiptoByCustomer(string DistributorCode, string customerCode);
        BaseResultModel GetDetailShiptoByShiptoId(string DistributorCode, Guid shiptoId);
        BaseResultModel GetRouteZoneBasicByPayload(string DistributorCode, DisRouteZoneBasicReqModel input);
        BaseResultModel GetEmployeesByShipto(string DistributorCode, string CustomerCode, string ShiptoCode);
    }
}