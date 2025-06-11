using DynamicSchema.Helper.Services;
using Microsoft.Extensions.Logging;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models.Distributor;
using ODSaleOrder.API.Services.Base;
using Sys.Common.Models;
using System.Linq;
using ODSaleOrder.API.Models.Common;
using ODSaleOrder.API.Models.Customer;
using ODSaleOrder.API.Models;
using System;

namespace ODSaleOrder.API.Services.Distributor
{
    public class DistributorService : IDistributorService
    {
        private readonly ILogger<DistributorService> _logger;
        private readonly IBaseRepository<Models.Common.EmployeeModel> _distributorEmployeeRepo;
        private readonly IBaseRepository<Models.Common.EmployeeV2Model> _distributorEmployeeV2Repo;
        private readonly IBaseRepository<DistributorCustomerModel> _distributorCustomerRepo;
        private readonly IBaseRepository<DisRouteZoneBasicModel> _disRouteZoneBasicRepo;
        private readonly IBaseRepository<DistributorCustomerWithPagingModel> _distributorCustomerPagingRepo;
        private readonly IBaseRepository<DistributorCustomerShiptoModel> _disCustomerShiptoRepo;
        private readonly IBaseRepository<DisCusShiptoDetailModel> _disCusShiptoDetailRepo;
        private readonly IBaseRepository<DistributorRouteZoneModel> _distributorRouteZoneRepo;
        private readonly IBaseRepository<DistributorBasicInfoModel> _distributorBasicInfoRepo;

        public DistributorService(ILogger<DistributorService> logger,
            IBaseRepository<Models.Common.EmployeeModel> distributorEmployeeRepo,
            IBaseRepository<DistributorCustomerModel> distributorCustomerRepo,
            IBaseRepository<DistributorRouteZoneModel> distributorRouteZoneRepo,
            IBaseRepository<DistributorBasicInfoModel> distributorBasicInfoRepo,
            IBaseRepository<DistributorCustomerWithPagingModel> distributorCustomerPagingRepo,
            IBaseRepository<DistributorCustomerShiptoModel> disCustomerShiptoRepo,
            IBaseRepository<DisCusShiptoDetailModel> disCusShiptoDetailRepo,
            IBaseRepository<DisRouteZoneBasicModel> disRouteZoneBasicRepo,
            IBaseRepository<Models.Common.EmployeeV2Model> distributorEmployeeV2Repo)
        {
            _logger = logger;
            _distributorEmployeeRepo = distributorEmployeeRepo;
            _distributorCustomerRepo = distributorCustomerRepo;
            _distributorRouteZoneRepo = distributorRouteZoneRepo;
            _distributorBasicInfoRepo = distributorBasicInfoRepo;
            _distributorCustomerPagingRepo = distributorCustomerPagingRepo;
            _disCustomerShiptoRepo = disCustomerShiptoRepo;
            _disCusShiptoDetailRepo = disCusShiptoDetailRepo;
            _disRouteZoneBasicRepo = disRouteZoneBasicRepo;
            _distributorEmployeeV2Repo = distributorEmployeeV2Repo;
        }

        public BaseResultModel GetBasicInfo(string DistributorCode)
        {
            var query = $@"SELECT 
                        dis.""Code"", 
                        dis.""Name"",
                        dis.""BussinessFullAddress"" as ""Address"",
                        dis.""Email"",
                        dis.""Phone""
                        FROM ""public"".""Distributors"" dis
                        WHERE dis.""Code"" = '{DistributorCode}' 
                        AND dis.""Status"" = '1' AND dis.""DeleteFlag"" = 0
                        AND (now() >= dis.""ValidFrom"" AND (dis.""ValidUntil""  >= now() OR dis.""ValidUntil"" is null));";
            var res = _distributorBasicInfoRepo.GetByFunction(query).ToList();
            if(res != null && res.Count > 0)
            {
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = res[0]
                };
            }

            return new BaseResultModel
            {
                IsSuccess = false,
                Code = 404,
                Message = "DistributorNotFound",
            };
        }

        public BaseResultModel GetSalemanList(string DistributorCode)
        {
            var query = $@"SELECT * FROM ""public"".""f_getemployeesbydistributor""('{DistributorCode}')";
            var res = _distributorEmployeeRepo.GetByFunction(query).ToList();
            return new BaseResultModel
            {
                IsSuccess = true,
                Code = 200,
                Message = "OK",
                Data = res
            };
        }

        public BaseResultModel GetShipperList(string DistributorCode)
        {
            var query = $@"SELECT * FROM ""public"".""f_getshipperbydistributor""('{DistributorCode}')";
            var res = _distributorEmployeeRepo.GetByFunction(query).ToList();
            return new BaseResultModel
            {
                IsSuccess = true,
                Code = 200,
                Message = "OK",
                Data = res
            };
        }

        public BaseResultModel GetRoutZoneList(string DistributorCode)
        {
            var query = $@"SELECT * FROM ""public"".""f_getroutezonebydistributor""('{DistributorCode}')";
            var res = _distributorRouteZoneRepo.GetByFunction(query).ToList();
            return new BaseResultModel
            {
                IsSuccess = true,
                Code = 200,
                Message = "OK",
                Data = res
            };
        }

        public BaseResultModel GetCustomerList(string DistributorCode)
        {
            var query = $@"SELECT * FROM ""public"".""f_getcustomerbydistributor""('{DistributorCode}')";
            var res = _distributorCustomerRepo.GetByFunction(query).ToList();
            return new BaseResultModel
            {
                IsSuccess = true,
                Code = 200,
                Message = "OK",
                Data = res
            };
        }

        public BaseResultModel GetCustomerListWithPaging(SearchCustomerModel input, string DistributorCode)
        {
            try
            {
                var query = $@"
                SELECT * FROM ""public"".""f_getcustomerbydistributorpaging""(
                    '{DistributorCode}',
                    {(string.IsNullOrEmpty(input.SearchValue) ? "NULL" : $"'{input.SearchValue}'")},
                    {input.PageNumber},
                    {input.PageSize}
                )";

                var res = _distributorCustomerPagingRepo.GetByFunction(query).ToList();

                int totalCount = res.Any() ? res.First().TotalCount : 0;
                int skip = (input.PageNumber - 1) * input.PageSize;
                int top = input.PageSize;

                var result = new PagedList<DistributorCustomerWithPagingModel>(res, totalCount, (skip / top) + 1, top);
                var repsonse = new ListDistributorCustomerModel { Items = result, MetaData = result.MetaData };

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = repsonse
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public BaseResultModel GetShiptoByCustomer(string DistributorCode, string customerCode)
        {
            try
            {
                var query = $@"
                SELECT * FROM ""public"".""f_getlistshiptobycustomer""(
                    '{DistributorCode}',
                    '{customerCode}'
                )";

                var res = _disCustomerShiptoRepo.GetByFunction(query).ToList();

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = res
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public BaseResultModel GetDetailShiptoByShiptoId(string DistributorCode, Guid shiptoId)
        {
            try
            {
                var query = $@"
                SELECT * FROM ""public"".""f_getdetailshipto""(
                    '{DistributorCode}',
                    '{shiptoId}'
                )";

                var res = _disCusShiptoDetailRepo.GetByFunction(query).FirstOrDefault();

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = res
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public BaseResultModel GetRouteZoneBasicByPayload(string DistributorCode, DisRouteZoneBasicReqModel input)
        {
            try
            {
                var query = $@"
                SELECT * FROM ""public"".""f_getroutezonebasicbypayload""(
                    '{DistributorCode}',
                    '{input.CustomerCode}',
                    '{input.ShiptoCode}',
                    '{input.DsaCode}'
                )";

                var res = _disRouteZoneBasicRepo.GetByFunction(query).FirstOrDefault();

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = res
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public BaseResultModel GetEmployeesByShipto(string DistributorCode, string CustomerCode, string ShiptoCode)
        {
            try
            {
                var query = $@"
                SELECT * FROM ""public"".""f_getemployeesbyshipto""(
                    '{DistributorCode}',
                    '{CustomerCode}',
                    '{ShiptoCode}'
                )";

                var res = _distributorEmployeeV2Repo.GetByFunction(query).ToList();

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = res
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
    }
}
