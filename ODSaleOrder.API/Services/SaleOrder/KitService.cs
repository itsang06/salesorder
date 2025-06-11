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

namespace ODSaleOrder.API.Services.SaleOrder
{
    public class KitService : IKitService
    {
        private readonly ILogger<KitService> _logger;
        private readonly IMapper _mapper;
        private readonly IBaseRepository<SO_Reason> _reasonRepository;

        public KitService(ILogger<KitService> logger,
            IMapper mapper,
            IBaseRepository<SO_Reason> reasonRepository)
        {
            _logger = logger;
            _mapper = mapper;
            _reasonRepository = reasonRepository;
        }

        public async Task<BaseResultModel> CalculateOrderHeaderByKitCode(SO_OrderItems item)
        {
            try
            {
               
                //return metadata
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                };

            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message  + " " + ex.StackTrace);
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> BulkUpsertReason(List<SO_Reason> models, string username)
        {
            try
            {
                foreach (var model in models)
                {
                    if (model.Id == null || model.Id == Guid.Empty)
                    {
                        model.CreatedBy = username;
                        model.CreatedDate = DateTime.Now;
                        _reasonRepository.Add(model);
                    }
                    else
                    {
                        model.UpdatedBy = username;
                        model.UpdatedDate = DateTime.Now;
                        _reasonRepository.UpdateUnSaved(model);
                    }
                }
                _reasonRepository.Save();
                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK",
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message  + " " + ex.StackTrace);
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
