using AutoMapper;
using Microsoft.Extensions.Logging;
using Sys.Common.Models;
using System;
using System.Linq;
using ODSaleOrder.API.Services.Base;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Services.Ffa.Interface;
using System.Collections.Generic;
using System.Reflection;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Models;
using DynamicSchema.Helper.Services;
using DynamicSchema.Helper.Services.Interface;
using static SysAdmin.API.Constants.Constant;
using ODSaleOrder.API.Models.SaleHistories;
using Microsoft.AspNetCore.Http;

namespace ODSaleOrder.API.Services.Ffa
{
    public class FfaSoOrderItemService : IFfaSoOrderItemService
    {
        private readonly ILogger<FfaSoOrderItemService> _logger;
        private readonly IMapper _mapper;
        protected readonly RDOSContext _dataContext;
        private readonly IBaseRepository<FfasoOrderItem> _ffaSoOrderItemsRepository;
        //private readonly IBaseRepository<FFAOrderItemExisted> _ffaOrderItemExistedRepository;

        // Private
        private readonly IDynamicBaseRepository<FFAOrderItemExisted> _ffaOrderItemExistedRepository;
        private readonly IDynamicBaseRepository<FfasoOrderItem> _ffaSoOrderItemRepository;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;

        public FfaSoOrderItemService(
            IHttpContextAccessor httpContextAccessor,
           ILogger<FfaSoOrderItemService> logger,
           IMapper mapper,
           RDOSContext dataContext,
           IBaseRepository<FfasoOrderItem> ffaSoOrderItemsRepository
           //IBaseRepository<FFAOrderItemExisted> ffaOrderItemExistedRepository
           )
        {
            _logger = logger;
            _mapper = mapper;
            _dataContext = dataContext;
            _ffaSoOrderItemsRepository = ffaSoOrderItemsRepository;
            //_ffaOrderItemExistedRepository = ffaOrderItemExistedRepository;

            // Private
            _ffaOrderItemExistedRepository = new DynamicBaseRepository<FFAOrderItemExisted>(dataContext);
            _ffaSoOrderItemRepository = new DynamicBaseRepository<FfasoOrderItem>(dataContext);

            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
        }

        public BaseResultModel CreateFfaSoOrderItem(FfasoOrderItem model, string token, string username)
        {
            try
            {
                model.Id = model.Id == Guid.Empty ? Guid.NewGuid() : model.Id;
                model.CreatedBy = username;
                model.CreatedDate = DateTime.Now;
                _ffaSoOrderItemsRepository.Add(model);
                _ffaSoOrderItemsRepository.Save();
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = model
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

        public BaseResultModel UpdateFfaSoOrderItem(FfasoOrderItem model, string token, string username)
        {
            try
            {
                var item = _ffaSoOrderItemsRepository.GetById(model.Id);
                if (item == null)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "SO not found"
                    };
                }

                model.UpdatedBy = username;
                model.UpdatedDate = DateTime.Now;
                _ffaSoOrderItemsRepository.UpdateUnSaved(model);
                _ffaSoOrderItemsRepository.Save();
                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK",
                    Data = model
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

        public BaseResultModel DeleteFfaSoOrderItem(Guid Id, string token, string username)
        {
            try
            {
                var item = _ffaSoOrderItemRepository.GetById(Id, _schemaName);
                if (item == null)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "SO not found"
                    };
                }
                item.IsDeleted = true;
                item.UpdatedBy = username;
                item.UpdatedDate = DateTime.Now;
                //_ffaSoOrderItemsRepository.UpdateUnSaved(item);
                _ffaSoOrderItemRepository.UpdateUnSaved(item, _schemaName);
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK"
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

        public BaseResultModel GetAll()
        {
            try
            {
                var items = _ffaSoOrderItemsRepository.GetAll().ToList();
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = items
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

        public BaseResultModel InsertMany(List<FfasoOrderItem> model, string token, string username)
        {
            try
            {

                TrySetProperty(model, "CreatedDate", DateTime.Now);
                TrySetProperty(model, "CreatedBy", username);

                return new BaseResultModel
                {
                    IsSuccess = _ffaSoOrderItemsRepository.InsertMany(model),
                    Code = 200,
                    Message = "OK",
                    Data = model
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

        public BaseResultModel UpdateMany(List<FfasoOrderItem> model, string token, string username)
        {
            try
            {
                TrySetProperty(model, "UpdatedDate", DateTime.Now);
                TrySetProperty(model, "UpdatedBy", username);
                
                return new BaseResultModel
                {
                    IsSuccess = _ffaSoOrderItemsRepository.UpdateMany(model),
                    Code = 200,
                    Message = "OK",
                    Data = model
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

        public BaseResultModel InsertOrUpdate(List<FfasoOrderItem> model)
        {
            try
            {
                var lstInsert = new List<FfasoOrderItem>();
                var lstUpdate = new List<FfasoOrderItem>();
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

                string _query = String.Format(@"SELECT src.""Id"", src.""OrderRefNumber"", src.""VisitId"" FROM ""{0}"" src WHERE src.""Id"" IN {1}", "FFASoOrderItems", requestId);
                var items = _ffaOrderItemExistedRepository.GetByFunction(_query, _schemaName).ToList();

                foreach (var item in model)
                {
                    if (items != null && items.Exists(k => k.Id == item.Id && string.IsNullOrEmpty(k.OrderRefNumber)))
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
                //var insertStatus = lstInsert.Count > 0 ? _ffaSoOrderItemsRepository.InsertMany(lstInsert) : true;
                //var updateStatus = lstUpdate.Count > 0 ? _ffaSoOrderItemsRepository.UpdateMany(lstUpdate) : true;
                var insertStatus = lstInsert.Count > 0 ? _ffaSoOrderItemRepository.InsertMany(lstInsert, _schemaName) : true;
                var updateStatus = lstUpdate.Count > 0 ? _ffaSoOrderItemRepository.UpdateMany(lstUpdate, _schemaName) : true;

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


        private bool TrySetProperty(object obj, string property, object value)
        {
            var prop = obj.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value, null);
                return true;
            }
            return false;
        }

        public BaseResultModel DeleteMany(List<string> model, string token, string username)
        {
            foreach (string id in model)
            {
                var item = _ffaSoOrderItemsRepository.GetById(Guid.Parse(id));
                if (item != null)
                {
                    item.IsDeleted = true;
                    item.UpdatedBy = username;
                    item.UpdatedDate = DateTime.Now;
                    _ffaSoOrderItemsRepository.UpdateUnSaved(item);
                }
            }

            return new BaseResultModel
            {
                IsSuccess = true,
                Code = 200,
                Message = "OK"
            };
        }

        public BaseResultModel DeleteByExternal_OrdNBR(string External_OrdNBR, string token, string username)
        {
            var item = _ffaSoOrderItemsRepository.GetAll().Where(x => x.External_OrdNBR.Equals(External_OrdNBR));
            _dataContext.RemoveRange(item);
            return new BaseResultModel
            {
                IsSuccess = true,
                Code = 200,
                Message = "OK"
            };
        }


        //------------------------------//

        public ResultCustomSale<List<FfasoOrderItem>> GetHistoryTransactions(SyncTransactionRequest request)
        {
            ResultCustomSale<List<FfasoOrderItem>> result = new ResultCustomSale<List<FfasoOrderItem>>();
            try
            {
                var _query = $@"select * from  ""FFA_Download_FFASoOrderItems""('{request.ConditionColumn}','{request.Period}','{request.EmployeeCode}','{request.DistributorCode}', 'SalesOrder', false)";
                result.Data = _ffaSoOrderItemsRepository.GetByFunction(_query).ToList();
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Messages.Add(ex.Message);
            }

            return result;
        }
    }
}
