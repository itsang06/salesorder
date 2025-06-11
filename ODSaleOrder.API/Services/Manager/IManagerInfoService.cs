using DynamicSchema.Helper.Models;

namespace ODSaleOrder.API.Services.Manager
{
    public interface IManagerInfoService
    {
        BaseResultModel GetDistributors(string EmployeeCode, string SaleOrg);
        BaseResultModel GetSalemanList(string EmployeeCode);
    }
}