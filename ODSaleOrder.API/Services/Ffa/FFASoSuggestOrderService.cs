using AutoMapper;
using Microsoft.Extensions.Logging;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Infrastructure;
using DynamicSchema.Helper.Services.Interface;
using Microsoft.AspNetCore.Http;
using ODSaleOrder.API.Models;
using static SysAdmin.API.Constants.Constant;
using DynamicSchema.Helper.Services;
using System.Collections.Generic;
using System;
using Sys.Common.Models;
using System.Linq;
using ODSaleOrder.API.Services.Ffa.Interface;

namespace ODSaleOrder.API.Services.Ffa
{
    public class FFASoSuggestOrderService : IFFASoSuggestOrderService
    {
        private readonly ILogger<FFASoSuggestOrderService> _logger;
        private readonly IMapper _mapper;
        protected readonly RDOSContext _dataContext;

        // Private
        private readonly IDynamicBaseRepository<FFASoSuggestOrder> _ffaSoSuggestOrderRepository;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;

        public FFASoSuggestOrderService(IHttpContextAccessor httpContextAccessor, ILogger<FFASoSuggestOrderService> logger, IMapper mapper, RDOSContext dataContext)
        {
            _logger = logger;
            _mapper = mapper;
            _dataContext = dataContext;

            // Private
            _ffaSoSuggestOrderRepository = new DynamicBaseRepository<FFASoSuggestOrder>(dataContext);

            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
        }

        public BaseResultModel InsertOrUpdate(List<FFASoSuggestOrder> model)
        {
            try
            {
                var lstInsert = new List<FFASoSuggestOrder>();
                var lstUpdate = new List<FFASoSuggestOrder>();
                BaseResultModel result = new BaseResultModel();


                List<string> arrayId = new();
                foreach (var item in model)
                {
                    if (item.Id != Guid.Empty)
                    {
                        arrayId.Add("'" + item.Id + "'");
                    }
                }
                var requestId = string.Join(", ", arrayId.ToArray());
                requestId = "(" + requestId + ")";

                string _query = String.Format(@"SELECT src.""Id"",  src.""VisitId"" FROM ""{0}"" src WHERE src.""Id"" IN {1}", "FFASoSuggestOrder", requestId);
                var items = _ffaSoSuggestOrderRepository.GetByFunction(_query, _schemaName).ToList();

                foreach (var item in model)
                {
                    if (items != null && items.Exists(k => k.Id == item.Id && string.IsNullOrEmpty(k.VisitId)))
                    {
                        item.UpdatedDate = DateTime.Now;
                        lstUpdate.Add(item);
                    }
                    else if (!items.Exists(k => k.Id == item.Id))
                    {
                        item.CreatedDate = DateTime.Now;
                        lstInsert.Add(item);
                    }
                }
                var insertStatus = lstInsert.Count > 0 ? _ffaSoSuggestOrderRepository.InsertMany(lstInsert, _schemaName) : true;
                var updateStatus = lstUpdate.Count > 0 ? _ffaSoSuggestOrderRepository.UpdateMany(lstUpdate, _schemaName) : true;

                result.IsSuccess = (!insertStatus || !updateStatus) ? false : true;
                result.Code = 200;
                result.Message = "OK";
                result.Data = model;

                return result;

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
