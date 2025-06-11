using AutoMapper;
using Microsoft.Extensions.Logging;
using Sys.Common.Models;
using System;
using System.Linq;
using ODSaleOrder.API.Services.Base;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Services.TotalSalesToDate.Interface;
using System.Collections.Generic;
using System.Reflection;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using DynamicSchema.Helper.Services.Interface;
using DynamicSchema.Helper.Services;
using System.Runtime.Intrinsics.X86;
using static SysAdmin.API.Constants.Constant;
using Microsoft.AspNetCore.Http;

namespace ODSaleOrder.API.Services.TotalSalesToDate
{


    public class TotalSalesToDateService : ITotalSalesToDateService
    {

        private readonly ILogger<TotalSalesToDateService> _logger;
        private readonly IMapper _mapper;
        protected readonly RDOSContext _dataContext;
        private readonly IDynamicBaseRepository<FfasoOrderInformation> _ffaSoOrderInformationsRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;

        public TotalSalesToDateService(
           ILogger<TotalSalesToDateService> logger,
           IHttpContextAccessor httpContextAccessor,
           IMapper mapper,
           RDOSContext dataContext
           )
        {
            _logger = logger;
            _mapper = mapper;
            _dataContext = dataContext;
            _ffaSoOrderInformationsRepository = new DynamicBaseRepository<FfasoOrderInformation>(dataContext);
            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
        }

        public BaseResultModelMobile GetTotalSalesToDate(string employeeCode, string token)
        {
            try
            {
                var listAll = _ffaSoOrderInformationsRepository.Find(o => o.SalesRepID == employeeCode && o.OrderDate.Value.Date == DateTime.Now.Date, _schemaName).ToList();
                //var list = from ffa in listAll
                //           where ffa.Created_By == employeeCode && ffa.OrderDate.Value.Date == DateTime.Now.Date
                //           select ffa;
                var item = listAll.Select(x => x.Orig_Ord_Amt ?? 0).Sum();
                return new BaseResultModelMobile
                {
                    success = true,
                    data = item
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new BaseResultModelMobile
                {
                    success = false,
                    message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
    }
}
