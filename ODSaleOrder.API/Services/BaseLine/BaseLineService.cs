using DynamicSchema.Helper.Services;
using DynamicSchema.Helper.Services.Interface;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using static SysAdmin.API.Constants.Constant;
using System.Threading.Tasks;
using System;
using Sys.Common.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ODSaleOrder.API.Services.SaleOrder;
using Microsoft.AspNetCore.Http;

namespace ODSaleOrder.API.Services.BaseLine
{
    public class BaseLineService : IBaseLineService
    {
        private readonly ILogger<BaseLineService> _logger;
        private readonly IDynamicBaseRepository<SO_SalesOrderSetting> _settingRepository;
        private readonly IDynamicBaseRepository<SaleCalendar> _saleCalendarRepo;
        private readonly IDynamicBaseRepository<SaleCalendarHoliday> _saleCalendarHolidayRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;
        public BaseLineService(RDOSContext dataContext, ILogger<BaseLineService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _settingRepository = new DynamicBaseRepository<SO_SalesOrderSetting>(dataContext);
            _saleCalendarRepo = new DynamicBaseRepository<SaleCalendar>(dataContext);
            _saleCalendarHolidayRepo = new DynamicBaseRepository<SaleCalendarHoliday>(dataContext);
            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
        }

        private async Task<ResultModelWithObject<int>> GetLeadDate()
        {
            try
            {
                var resultData = await _settingRepository
                    .GetAllQueryable(null, null, null, _schemaName)
                    .FirstOrDefaultAsync();

                if (IsODSiteConstant)
                {
                    return new ResultModelWithObject<int>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = resultData.DeliveryLeadDate
                    };
                }
                else
                {
                    return new ResultModelWithObject<int>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = resultData.LeadDate
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<int>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
        // Calculate date filter Order
        public async Task<BaseResultModel> HandleCalculateBaselineDate()
        {
            var resultBaselineDate = await GetLeadDate();
            if (!resultBaselineDate.IsSuccess)
            {
                return new BaseResultModel
                {
                    Code = resultBaselineDate.Code,
                    Message = resultBaselineDate.Message,
                    IsSuccess = false
                };
            }
            int leadDate = resultBaselineDate.Data;
            var baselineDateCurrent = DateTime.Now;
            var saleCalendar = await _saleCalendarRepo.GetAllQueryable(x => x.SaleYear == baselineDateCurrent.Year).FirstOrDefaultAsync();
            if (saleCalendar == null)
            {
                return new BaseResultModel
                {
                    Code = 400,
                    Message = "Sale Calendar not found",
                    IsSuccess = false
                };
            }
            for (int i = 1; i <= leadDate; i++)
            {
                var date = DateTime.Now.AddDays(-i);
                //var date = new DateTime(2023, 06, 26).AddDays(-i);
                if (date.Year != saleCalendar.SaleYear)
                {
                    saleCalendar = await _saleCalendarRepo.GetAllQueryable(x => x.SaleYear == date.Year).FirstOrDefaultAsync();
                }

                if (((int)date.DayOfWeek == 6 || date.DayOfWeek == 0) && saleCalendar.IncludeWeekend == null)
                {
                    leadDate += 1;
                    continue;
                }
                else if (date.DayOfWeek == 0 && saleCalendar.IncludeWeekend == "SAT")
                {
                    leadDate += 1;
                    continue;
                }

                if (_saleCalendarHolidayRepo.GetAllQueryable(x => x.HolidayDate.Date == date.Date).Any())
                {
                    leadDate += 1;
                    continue;
                }
            }
            baselineDateCurrent = baselineDateCurrent.AddDays(-leadDate);

            return new BaseResultModel
            {
                IsSuccess = true,
                Message = "Success",
                Data = baselineDateCurrent,
                Code = 200
            };
        }
    }
}
