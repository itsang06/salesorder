using AutoMapper;
using DynamicSchema.Helper.Services;
using DynamicSchema.Helper.Services.Interface;
using Elasticsearch.Net;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.DistributorSalesOrder;
using ODSaleOrder.API.Models.SaleHistories;
using ODSaleOrder.API.Services.Base;
using ODSaleOrder.API.Services.Ffa.Interface;
using Sys.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static SysAdmin.API.Constants.Constant;

namespace ODSaleOrder.API.Services.Ffa
{


    public class FfaSoOrderInformationService : IFfaSoOrderInformationService
    {

        private readonly ILogger<FfaSoOrderInformationService> _logger;
        private readonly IMapper _mapper;
        protected readonly RDOSContext _dataContext;
        private readonly IBaseRepository<FfasoOrderInformation> _ffaSoOrderInformationsRepository;
        //private readonly IBaseRepository<FFAOrderInfoExisted> _ffaOrderInfoExistedRepository;

        // Private
        private readonly IDynamicBaseRepository<FFAOrderInfoExisted> _ffaOrderInfoExistedRepository;
        private readonly IDynamicBaseRepository<FfasoOrderInformation> _ffaSoOrderRepository;
        private readonly IDynamicBaseRepository<FfasoOrderItem> _ffaSoOrderItemRepository;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;
        public FfaSoOrderInformationService(
           IHttpContextAccessor httpContextAccessor,
           ILogger<FfaSoOrderInformationService> logger,
           IMapper mapper,
           RDOSContext dataContext,
           IBaseRepository<FfasoOrderInformation> ffaSoOrderInformationsRepository
           //IBaseRepository<FFAOrderInfoExisted> ffOrderInfoExistedModelRepository
           )
        {
            _logger = logger;
            _mapper = mapper;
            _dataContext = dataContext;
            _ffaSoOrderInformationsRepository = ffaSoOrderInformationsRepository;
            //_ffaOrderInfoExistedRepository = ffOrderInfoExistedModelRepository;

            // Private
            _ffaOrderInfoExistedRepository = new DynamicBaseRepository<FFAOrderInfoExisted>(dataContext);
            _ffaSoOrderRepository = new DynamicBaseRepository<FfasoOrderInformation>(dataContext);
            _ffaSoOrderItemRepository = new DynamicBaseRepository<FfasoOrderItem>(dataContext);

            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
        }

        public BaseResultModel CreateFfaSoOrderInformation(FfasoOrderInformation model, string token, string username)
        {
            try
            {
                model.Id = Guid.NewGuid();
                model.CreatedBy = username;
                model.CreatedDate = DateTime.Now;
                _ffaSoOrderInformationsRepository.Add(model);
                _ffaSoOrderInformationsRepository.Save();
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

        public BaseResultModel DeleteFfaSoOrderInformation(Guid Id, string token, string username)
        {
            try
            {
                //var item = _ffaSoOrderInformationsRepository.GetById(Id);
                var item = _ffaSoOrderRepository.GetById(Id, _schemaName);
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
                //_ffaSoOrderInformationsRepository.UpdateUnSaved(item);
                _ffaSoOrderRepository.UpdateUnSaved(item, _schemaName);
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

        public BaseResultModel UpdateFfaSoOrderInformation(FfasoOrderInformation model, string token, string username)
        {
            try
            {
                var item = _ffaSoOrderInformationsRepository.GetById(model.Id);
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
                _ffaSoOrderInformationsRepository.UpdateUnSaved(model);
                _ffaSoOrderInformationsRepository.Save();
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

        public BaseResultModel GetAll()
        {
            try
            {
                var items = _ffaSoOrderInformationsRepository.GetAll().ToList();
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

       
        public BaseResultModel InsertMany(List<FfasoOrderInformation> model, string token, string username)
        {
            try
            {

                TrySetProperty(model, "CreatedDate", DateTime.Now);
                TrySetProperty(model, "CreatedBy", username);

                return new BaseResultModel
                {
                    IsSuccess = _ffaSoOrderInformationsRepository.InsertMany(model),
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

        public BaseResultModel UpdateMany(List<FfasoOrderInformation> model, string token, string username)
        {
            try
            {
                TrySetProperty(model, "UpdatedDate", DateTime.Now);
                TrySetProperty(model, "UpdatedBy", username);
                
                return new BaseResultModel
                {
                    IsSuccess = _ffaSoOrderInformationsRepository.UpdateMany(model),
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


        public BaseResultModel InsertOrUpdate(List<FfasoOrderInformation> model)
        {
            try
            {
                var lstInsert = new List<FfasoOrderInformation>();
                var lstUpdate = new List<FfasoOrderInformation>();
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

                string _query = String.Format(@"SELECT src.""Id"", src.""OrderRefNumber"", src.""VisitID"", src.""Status"" FROM ""{0}"" src WHERE src.""Id"" IN {1}", 
                                               "FFASoOrderInformations", requestId);
                var items = _ffaOrderInfoExistedRepository.GetByFunction(_query, _schemaName).ToList();

                foreach (var item in model)
                { 
                     if (items != null && items.Exists(k => k.Id == item.Id && k.Status == FFASOSTATUS.WatingImport))
                    {
                        item.UpdatedDate = DateTime.Now;
                        lstUpdate.Add(item);
                    }
                    else if(!items.Exists(k => k.Id == item.Id))
                    {      
                        item.CreatedDate = DateTime.Now;
                        lstInsert.Add(item);
                    }
                   
                }
                //var insertStatus = _ffaSoOrderInformationsRepository.InsertMany(lstInsert);
                //var updateStatus = _ffaSoOrderInformationsRepository.UpdateMany(lstUpdate);
                var insertStatus = _ffaSoOrderRepository.InsertMany(lstInsert, _schemaName);
                var updateStatus = _ffaSoOrderRepository.UpdateMany(lstUpdate, _schemaName);

                result.IsSuccess = (!insertStatus || !updateStatus) ? false : true;
                result.Code = 200;
                result.Message = "OK";
                result.Data = model;

                return result;
                
            }
            catch (Exception ex)
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

        public FfasoOrderInformation GetByVisitId(string visitId)
        {
            return _ffaSoOrderInformationsRepository.Find(x => x.VisitID == visitId).FirstOrDefault();
        }

        public FfasoOrderInformation GetByExternalOrdNBR(string externalOrdNBR)
        {
            return _ffaSoOrderInformationsRepository.Find(x => x.External_OrdNBR == externalOrdNBR).FirstOrDefault();
        }

        public FfasoOrderInformation GetByOrderRefNumber(string orderRefNumber)
        {
            return _ffaSoOrderInformationsRepository.Find(x => x.OrderRefNumber == orderRefNumber).FirstOrDefault();
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

        //------------------------------//
        public ResultCustomSale<List<FfasoOrderInformation>> GetHistoryTransactions(SyncTransactionRequest request)
        {
            ResultCustomSale<List<FfasoOrderInformation>> result = new ResultCustomSale<List<FfasoOrderInformation>>();
            try
            {
                var _query = $@"select * from  ""FFA_Download_FFASoOrderInformations""('{request.ConditionColumn}','{request.Period}','{request.EmployeeCode}','{request.DistributorCode}', 'SalesOrder', false)";
                result.Data = _ffaSoOrderInformationsRepository.GetByFunction(_query).ToList();
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Messages.Add(ex.Message);
            }

            return result;
        }

        //------------------------------//
        public BaseResultModel GetOrderDetailByVisitId(string VisitId)
        {
            try
            {
                BaseResultModel result = new BaseResultModel();

                if (string.IsNullOrWhiteSpace(VisitId))
                {
                    result.IsSuccess = false;
                    result.Code = 400;
                    result.Message = "VisitId is null.";

                    return result;
                }


                var orderInfo = _ffaSoOrderRepository.Find(o => o.VisitID == VisitId, _schemaName).FirstOrDefault();
                if (orderInfo == null)
                {
                    result.Data = null;
                    result.IsSuccess = false;
                    result.Code = 404;
                    result.Message = "Order can not found.";

                    return result;
                }

                FFASaleOrderModel disOrder = new FFASaleOrderModel();
                disOrder.OrderInformations = orderInfo;
                disOrder.OrderItems = _ffaSoOrderItemRepository.Find(x => x.External_OrdNBR == orderInfo.External_OrdNBR, _schemaName).ToList();

                result.Data = disOrder;
                result.IsSuccess = true;
                result.Code = 200;
                result.Message = "OK";

                return result;
            }
            catch (Exception ex)
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
