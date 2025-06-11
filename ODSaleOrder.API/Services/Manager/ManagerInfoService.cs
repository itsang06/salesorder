using DynamicSchema.Helper.Models;
using Microsoft.Extensions.Logging;
using ODSaleOrder.API.Models.Common;
using ODSaleOrder.API.Models.Distributor;
using ODSaleOrder.API.Services.Base;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ODSaleOrder.API.Services.Manager
{
    public class ManagerInfoService : IManagerInfoService
    {
        private readonly ILogger<ManagerInfoService> _logger;
        private readonly IBaseRepository<DistributorCommonInfoModel> _distributorRepo;
        private readonly IBaseRepository<EmployeeModel> _managerEmployeeRepo;

        public ManagerInfoService(ILogger<ManagerInfoService> logger,
           IBaseRepository<DistributorCommonInfoModel> distributorRepo,
           IBaseRepository<EmployeeModel> managerEmployeeRepo)
        {
            _logger = logger;
            _distributorRepo = distributorRepo;
            _managerEmployeeRepo = managerEmployeeRepo;
        }

        public BaseResultModel GetSalemanList(string EmployeeCode)
        {
            var query = $@"SELECT * FROM ""public"".""f_getemployeesbymanager""('{EmployeeCode}')";
            var res = _managerEmployeeRepo.GetByFunction(query).ToList();
            return new BaseResultModel
            {
                IsSuccess = true,
                Code = 200,
                Message = "OK",
                Data = res
            };
        }

        public BaseResultModel GetDistributors(string EmployeeCode, string SaleOrg)
        {
            var query = $@"SELECT  distributorcode as ""Code"", distributorname as ""Name"" FROM ""public"".""CM_Filter_GetDistributorlist""('{EmployeeCode}', '{SaleOrg}');";
            var res = _distributorRepo.GetByFunction(query).ToList();
            return new BaseResultModel
            {
                IsSuccess = true,
                Code = 200,
                Message = "OK",
                Data = res
            };
        }

    }
}
