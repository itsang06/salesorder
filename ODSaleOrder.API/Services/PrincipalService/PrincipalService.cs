using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models.PrincipalModel;
using static SysAdmin.API.Constants.Constant;
using Sys.Common.Models;
using System.Threading.Tasks;
using System;
using DynamicSchema.Helper.Services.Interface;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using DynamicSchema.Helper.Services;
using Microsoft.EntityFrameworkCore;

namespace ODSaleOrder.API.Services.PrincipalService
{
    public class PrincipalService : IPrincipalService
    {
        private readonly IDynamicBaseRepository<Principal> _principalRepo;
        private readonly string _principleCode;
        private ISchemaNavigateService<ODDistributorSchema> _schemaNavigateService;
        public PrincipalService(RDOSContext context)
        {
            _principalRepo = new DynamicBaseRepository<Principal>(context);
            _principleCode = Environment.GetEnvironmentVariable("PRINCIPALCODE");
            _schemaNavigateService = new SchemaNavigateService<ODDistributorSchema>(context);
        }

        public async Task<bool> IsODValidation()
        {
            var principal = await _principalRepo.GetAllQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Code == _principleCode);
            IsODSiteConstant = principal != null ? principal.IsODSystem.Value : false;
            LinkODSystem = principal.LinkODSystem;
            return IsODSiteConstant;
        }
        public async Task<BaseResultModel> NavigatePrivateSchema(string distributorCode)
        {
            OD_Constant.SchemaName = OD_Constant.DEFAULT_SCHEMA;
            if (distributorCode == null || string.IsNullOrEmpty(distributorCode))
            {
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 400,
                    Message = "Header DistributorCode cannot null"
                };
            }

            var resultNavigate = await _schemaNavigateService.NavigateSchemaByDistributorCode(distributorCode);
            if (!resultNavigate.IsSuccess)
            {
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = resultNavigate.Code,
                    Message = resultNavigate.Message
                };
            }

            lock (typeof(OD_Constant))
            {
                OD_Constant.SchemaName = resultNavigate.Data.SchemaName;
                OD_Constant.DistributorCode = resultNavigate.Data.DistributorCode;
            }

            return new BaseResultModel
            {
                IsSuccess = true,
                Code = 200,
                Data = resultNavigate.Data
            };
        }
    }
}
