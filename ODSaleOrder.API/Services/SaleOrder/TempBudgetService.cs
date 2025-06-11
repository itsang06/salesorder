using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sys.Common.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Sys.Common.Helper;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using ODSaleOrder.API.Services.Base;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using static SysAdmin.API.Constants.Constant;
using Newtonsoft.Json.Linq;
using Sys.Common.Utils;
using DynamicSchema.Helper.Services.Interface;
using DynamicSchema.Helper.Services;
using Microsoft.AspNetCore.Http;

namespace ODSaleOrder.API.Services.SaleOrder
{
    public class TempBudgetService : ITempBudgetService
    {
        private readonly ILogger<TempBudgetService> _logger;
        private readonly IMapper _mapper;
        private readonly IDynamicBaseRepository<Temp_SOBudgets> _budgetRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;

        public TempBudgetService(ILogger<TempBudgetService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            RDOSContext db
        )
        {
            _logger = logger;
            _mapper = mapper;
            _budgetRepository = new DynamicBaseRepository<Temp_SOBudgets>(db);
            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
        }

        public async Task<ResultModelWithObject<TempBudgetModel>> Search(SearchBudgetModel parameters)
        {
            try
            {
                if (parameters.PageNumber <= 0) parameters.PageNumber = 1;

                var query = _budgetRepository.GetAllQueryable(null, null, null, _schemaName).Where(x => !x.IsDeleted).AsNoTracking();

                var res = query.ToList();

                if (!string.IsNullOrWhiteSpace(parameters.SearchValue))
                {
                    res = res.Where(x =>
                        (!string.IsNullOrEmpty(x.BudgetCode) && x.BudgetCode.ToLower().Contains(parameters.SearchValue.ToLower()))).ToList();
                }

                if (!string.IsNullOrWhiteSpace(parameters.CustomerCode))
                {
                    res = res.Where(x =>
                        (!string.IsNullOrEmpty(x.CustomerCode) && x.CustomerCode == parameters.CustomerCode)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(parameters.CustomerShiptoCode))
                {
                    res = res.Where(x =>
                        (!string.IsNullOrEmpty(x.CustomerShiptoCode) && x.CustomerShiptoCode == parameters.CustomerShiptoCode)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(parameters.PromotionCode))
                {
                    res = res.Where(x =>
                        (!string.IsNullOrEmpty(x.PromotionCode) && x.PromotionCode == parameters.PromotionCode)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(parameters.PromotionLevel))
                {
                    res = res.Where(x =>
                        (!string.IsNullOrEmpty(x.PromotionLevel) && x.PromotionLevel == parameters.PromotionLevel)).ToList();
                }

                if (!string.IsNullOrWhiteSpace(parameters.BudgetCode))
                {
                    res = res.Where(x =>
                        (!string.IsNullOrEmpty(x.BudgetCode) && x.BudgetCode == parameters.BudgetCode)).ToList();
                }

                res = res.OrderByDescending(x => x.CreatedDate).ToList();
                var listRespone = res.ToList();
                _budgetRepository.Dispose();
                if (parameters.IsDropdown)
                {
                    var page1 = PagedList<Temp_SOBudgets>.ToPagedList(listRespone, 0, listRespone.Count);

                    var reponse = new TempBudgetModel { Items = page1 };
                    return new ResultModelWithObject<TempBudgetModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = reponse
                    };
                }

                var poTempPagged = PagedList<Temp_SOBudgets>.ToPagedList(listRespone, (parameters.PageNumber - 1) * parameters.PageSize, parameters.PageSize);
                var repsonse = new TempBudgetModel { Items = poTempPagged, MetaData = poTempPagged.MetaData };

                //return metadata
                return new ResultModelWithObject<TempBudgetModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = repsonse
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<TempBudgetModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
    }
}
