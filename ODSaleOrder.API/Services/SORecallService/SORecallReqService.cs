using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models.SORecallModels;
using ODSaleOrder.API.Services.Base;
using ODSaleOrder.API.Services.SaleOrder;
using RestSharp.Authenticators;
using RestSharp;
using Sys.Common.Helper;
using Sys.Common.Models;
using SysAdmin.API.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static SysAdmin.API.Constants.Constant;
using static SysAdmin.Models.StaticValue.CommonData;
using ODSaleOrder.API.Models.SyncHistory;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using DynamicSchema.Helper.Services.Interface;
using DynamicSchema.Helper.Services;
using Google.Apis.Services;
using ODSaleOrder.API.Models;
using System.Linq.Dynamic.Core.Tokenizer;
using SyncToStaging.Helper.Services;

namespace ODSaleOrder.API.Services.SORecallService
{
    public class SORecallReqService : ISORecallReqService
    {
        // Service
        private readonly ILogger<SORecallReqService> _logger;
        private readonly IMapper _mapper;
        private readonly string _principleCode;
        private readonly ISyncCommonService _commonService;
        private readonly SaleOrder.Interface.IClientService _clientService;

        private readonly IDynamicBaseRepository<SoorderRecallReq> _soOrderRecallReqRepo;
        private readonly IDynamicBaseRepository<SoorderRecallReqScope> _soOrderRecallReqScopeRepo;
        private readonly IDynamicBaseRepository<SoorderRecallReqGiveBack> _soOrderRecallReqGiveBackRepo;
        private readonly IDynamicBaseRepository<SoorderRecallReqOrder> _soOrderRecallReqOrderRepo;

        private readonly RDOSContext _db;

        public SORecallReqService(
            ILogger<SORecallReqService> logger,
            IMapper mapper,
            ISyncCommonService commonService,
            RDOSContext db,
            SaleOrder.Interface.IClientService clientService
        )
        {
            _db = db;
            _clientService = clientService;
            _logger = logger;
            _soOrderRecallReqRepo = new DynamicBaseRepository<SoorderRecallReq>(_db);
            _soOrderRecallReqScopeRepo = new DynamicBaseRepository<SoorderRecallReqScope>(_db);
            _soOrderRecallReqGiveBackRepo = new DynamicBaseRepository<SoorderRecallReqGiveBack>(_db);
            _soOrderRecallReqOrderRepo = new DynamicBaseRepository<SoorderRecallReqOrder>(_db);
            _mapper = mapper;
            _principleCode = Environment.GetEnvironmentVariable("PRINCIPALCODE");
            _commonService = commonService;
        }

        #region Recall Request
        public async Task<BaseResultModel> ValidateCommon(SORecallReqModel model)
        {
            try
            {
                if (model.Status != SORECALLSTATUS.NEW &&
                    model.Status != SORECALLSTATUS.RELEASED)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "Status code is incorrect",
                        Code = 400
                    };
                }

                if (model.RecallProductType != SORECALLTYPE.SKU &&
                    model.RecallProductType != SORECALLTYPE.ITEMATTRIBUTE &&
                    model.RecallProductType != SORECALLTYPE.ITEMGROUP)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "Recall product type code is incorrect",
                        Code = 400
                    };
                }

                if (model.GiveBackProductType != SORECALLTYPE.SKU &&
                    model.GiveBackProductType != SORECALLTYPE.ITEMATTRIBUTE &&
                    model.GiveBackProductType != SORECALLTYPE.ITEMGROUP)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "Give back product type code is incorrect",
                        Code = 400
                    };
                }

                if (model.ScopeType != SORECALLSCOPETYPE.SALEAREA &&
                    model.ScopeType != SORECALLSCOPETYPE.DISTRIBUTOR)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "Scope type code is incorrect",
                        Code = 400
                    };
                }

                if (model.Status == SORECALLSTATUS.RELEASED)
                {
                    if (model.ListGiveBack.Count == 0)
                    {
                        return new BaseResultModel
                        {
                            IsSuccess = false,
                            Message = "List give back cannot null",
                            Code = 400
                        };
                    }
                }

                if (model.ListGiveBack.Count > 0 &&
                    !model.ListGiveBack.Any(x => x.IsDefault == true))
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "Cannot found product default in list product give back",
                        Code = 400
                    };
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Message = "Validated",
                    Code = 200
                };

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
        public async Task<BaseResultModel> InsertOrderRequest(SORecallReqModel model, string username, string token, bool isSync = false)
        {
            try
            {
                // Validate
                BaseResultModel validateResult = await ValidateCommon(model);
                if (!validateResult.IsSuccess) return validateResult;

                //Generate RefNumber
                var prefix = StringsHelper.GetPrefixYYM();
                var refNumber = await _soOrderRecallReqRepo.GetAllQueryable()
                    .Where(x => x.Code.Contains(prefix)).AsNoTracking().Select(x => x.Code).OrderByDescending(x => x).FirstOrDefaultAsync();
                var generatedNumber = StringsHelper.GennerateCodeWithYearMonthFormat(prefix, refNumber != null ? refNumber : null);

                string _ownerType = OwnerTypeConstant.SYSTEM;
                string _ownerCode = null;

                List<string> territoryValues = new List<string>();
                List<string> distributorCodes = new List<string>();


                if (isSync)
                {
                    model.ExternalCode = model.Code;
                    _ownerType = OwnerTypeConstant.PRINCIPAL;
                    _ownerCode = model.OwnerCode;
                }

                model.Id = Guid.NewGuid();
                model.Code = generatedNumber;
                model.CreatedBy = username;
                model.CreatedDate = DateTime.Now;
                model.UpdatedBy = null;
                model.UpdatedDate = null;
                model.OwnerType = _ownerType;
                model.OwnerCode = _ownerCode;
                model.IsDeleted = false;
                model.IsSync = isSync;

                _soOrderRecallReqRepo.Add(model);

                foreach (var scope in model.ListScope)
                {
                    scope.Id = Guid.NewGuid();
                    scope.CreatedBy = username;
                    scope.CreatedDate = DateTime.Now;
                    scope.OwnerType = _ownerType;
                    scope.OwnerCode = _ownerCode;
                    scope.RecallReqCode = model.Code;
                    scope.IsDeleted = false;
                    scope.UpdatedBy = null;
                    scope.UpdatedDate = null;

                    if (model.ScopeType == SORECALLSCOPETYPE.DISTRIBUTOR)
                    {
                        distributorCodes.Add(scope.Code);
                    }
                    else
                    {
                        territoryValues.Add(scope.Code);
                    }
                }

                _soOrderRecallReqScopeRepo.AddRange(model.ListScope);

                model.ListGiveBack.ForEach(x =>
                {
                    x.Id = Guid.NewGuid();
                    x.CreatedBy = username;
                    x.CreatedDate = DateTime.Now;
                    x.OwnerType = _ownerType;
                    x.OwnerCode = _ownerCode;
                    x.RecallReqCode = model.Code;
                    x.IsDeleted = false;
                    x.UpdatedBy = null;
                    x.UpdatedDate = null;
                });

                _soOrderRecallReqGiveBackRepo.AddRange(model.ListGiveBack);

                if (isSync)
                {
                    FilterRawSO reqBaseLine = new FilterRawSO();
                    reqBaseLine.SalesOrgCode = model.SaleOrgCode;
                    reqBaseLine.TerritoryLevel = model.SaleTerritoryLevel;
                    reqBaseLine.TerritoryValues = territoryValues;
                    reqBaseLine.DistributorCodes = distributorCodes;
                    reqBaseLine.FromDate = model.OrderDateFrom;
                    reqBaseLine.ToDate = model.OrderDateTo;

                    if (model.RecallProductType == SORECALLTYPE.ITEMATTRIBUTE)
                    {
                        reqBaseLine.ItemAttributeLevel = model.RecallProductLevel;
                        reqBaseLine.ItemAttributeCode = model.RecallProductCode;
                    }
                    else if (model.RecallProductType == SORECALLTYPE.ITEMGROUP)
                    {
                        reqBaseLine.ItemGroupCode = model.RecallProductCode;
                    }
                    else
                    {
                        reqBaseLine.ItemCode = model.RecallProductCode;
                    }

                    // nếu data sync từ CP qua OD sẽ collect data RowSO
                    ResultModelWithObject<ListRawSO> _result =
                            await _clientService.CommonRequestAsync<ResultModelWithObject<ListRawSO>>(
                                SystemUrlCode.ODBaseLineAPI,
                                $"/rawso/exfilterrawso",
                                Method.POST,
                                token,
                                reqBaseLine
                            );

                    if (_result == null)
                    {
                        return new BaseResultModel
                        {
                            IsSuccess = false,
                            Message = "API OD BaseLine cannot working",
                            Code = 500
                        };
                    }

                    if (!_result.IsSuccess)
                    {
                        return new BaseResultModel
                        {
                            IsSuccess = false,
                            Message = _result.Message,
                            Code = _result.Code
                        };
                    }

                    List<SoorderRecallReqOrder> _listOrder = new List<SoorderRecallReqOrder>();

                    foreach (var order in _result.Data.Items)
                    {
                        SoorderRecallReqOrder orderMap = new();
                        orderMap.CustomerCode = order.CustomerId;
                        orderMap.CustomerName = order.CustomerName;
                        orderMap.CustomerShiptoCode = order.CustomerShiptoId;
                        orderMap.DistributorCode = order.DistributorId;
                        orderMap.WarehouseId = order.WarehouseId;
                        orderMap.LocationId = order.LocationId;
                        orderMap.SalesRepId = order.SalesRepId;
                        orderMap.SalesRepEmpName = order.SalesRepEmpName;
                        orderMap.Status = order.Status;
                        orderMap.OrderCode = order.OrderRefNumber;
                        orderMap.ItemCode = order.InventoryId;
                        orderMap.ItemDescription = order.InventoryDescription;
                        orderMap.Uom = order.Uom;
                        orderMap.OrderQuantity = order.ShippedQuantities;
                        orderMap.OrderBaseQuantity = order.ShippedBaseQuantities;
                        orderMap.OrderDate = order.TransactionDate;
                        _listOrder.Add(orderMap);
                    }

                    model.ListOrder = _listOrder;
                }


                model.ListOrder.ForEach(x =>
                {
                    x.Id = Guid.NewGuid();
                    x.CreatedBy = username;
                    x.CreatedDate = DateTime.Now;
                    x.OwnerType = _ownerType;
                    x.OwnerCode = _ownerCode;
                    x.RecallReqCode = model.Code;
                    x.IsDeleted = false;
                    x.UpdatedBy = null;
                    x.UpdatedDate = null;
                    x.IsRecall = false;
                    x.RecallCode = null;
                });

                _soOrderRecallReqOrderRepo.AddRange(model.ListOrder);
                _soOrderRecallReqRepo.Save();

                if (!IsODSiteConstant && model.Status == SORECALLSTATUS.RELEASED)
                {
                    BaseResultModel syncDataToOD = await SyncDataToOD(model, token);
                    if (!syncDataToOD.IsSuccess) return syncDataToOD;
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Message = "Successfully",
                    Code = 201,
                    Data = (await GetDetailReq(generatedNumber)).Data
                };
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

        public async Task<BaseResultModel> UpdateOrderRequest(SORecallReqModel model, string username, string token)
        {
            try
            {
                // Check data update
                SoorderRecallReq dataInDb = await _soOrderRecallReqRepo
                    .GetAllQueryable()
                    .FirstOrDefaultAsync(x => x.Code == model.Code && !x.IsDeleted);

                if (dataInDb == null)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 404,
                        Message = "Cannot found order recall request"
                    };
                }

                if (dataInDb.Status == SORECALLSTATUS.RELEASED)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "Cannot edit order has been released"
                    };
                }

                // Validate
                BaseResultModel validateResult = await ValidateCommon(model);
                if (!validateResult.IsSuccess) return validateResult;

                string createdBy = dataInDb.CreatedBy;
                DateTime? createdDate = dataInDb.CreatedDate;
                string ownerType = dataInDb.OwnerType;
                string ownerCode = dataInDb.OwnerCode;
                Guid idTemp = dataInDb.Id;
                bool isSync = dataInDb.IsSync;
                //bool isSync = dataInDb.IsSync;

                _mapper.Map(model, dataInDb);

                dataInDb.Id = idTemp;
                dataInDb.CreatedDate = createdDate;
                dataInDb.OwnerType = ownerType;
                dataInDb.OwnerCode = ownerCode;
                dataInDb.CreatedBy = createdBy;
                dataInDb.UpdatedBy = username;
                dataInDb.UpdatedDate = DateTime.Now;
                dataInDb.IsSync = isSync;

                _soOrderRecallReqRepo.UpdateUnSaved(dataInDb);

                // List scope
                List<SoorderRecallReqScope> sORecallReqScopeInDbs = await _soOrderRecallReqScopeRepo.GetAllQueryable(x => x.RecallReqCode == dataInDb.Code).ToListAsync();

                if (sORecallReqScopeInDbs.Count > 0)
                {
                    _soOrderRecallReqScopeRepo.RemoveRange(sORecallReqScopeInDbs);
                }

                model.ListScope.ForEach(x =>
                {
                    x.Id = Guid.NewGuid();
                    x.CreatedBy = username;
                    x.CreatedDate = DateTime.Now;
                    x.OwnerType = OwnerTypeConstant.SYSTEM;
                    x.OwnerCode = null;
                    x.RecallReqCode = model.Code;
                    x.IsDeleted = false;
                    x.UpdatedBy = null;
                    x.UpdatedDate = null;
                });

                _soOrderRecallReqScopeRepo.AddRange(model.ListScope);

                // List give back
                List<SoorderRecallReqGiveBack> sORecallReqGiveBackInDbs = await _soOrderRecallReqGiveBackRepo.GetAllQueryable(x => x.RecallReqCode == dataInDb.Code).ToListAsync();

                if (sORecallReqGiveBackInDbs.Count > 0)
                {
                    _soOrderRecallReqGiveBackRepo.RemoveRange(sORecallReqGiveBackInDbs);
                }

                model.ListGiveBack.ForEach(x =>
                {
                    x.Id = Guid.NewGuid();
                    x.CreatedBy = username;
                    x.CreatedDate = DateTime.Now;
                    x.OwnerType = OwnerTypeConstant.SYSTEM;
                    x.OwnerCode = null;
                    x.RecallReqCode = model.Code;
                    x.IsDeleted = false;
                    x.UpdatedBy = null;
                    x.UpdatedDate = null;
                });

                _soOrderRecallReqGiveBackRepo.AddRange(model.ListGiveBack);

                // List order
                List<SoorderRecallReqOrder> sORecallReqOrderInDbs = await _soOrderRecallReqOrderRepo.GetAllQueryable(x => x.RecallReqCode == dataInDb.Code).ToListAsync();

                if (sORecallReqOrderInDbs.Count > 0)
                {
                    _soOrderRecallReqOrderRepo.RemoveRange(sORecallReqOrderInDbs);
                }

                model.ListOrder.ForEach(x =>
                {
                    x.Id = Guid.NewGuid();
                    x.CreatedBy = username;
                    x.CreatedDate = DateTime.Now;
                    x.OwnerType = OwnerTypeConstant.SYSTEM;
                    x.OwnerCode = null;
                    x.RecallReqCode = model.Code;
                    x.IsDeleted = false;
                    x.UpdatedBy = null;
                    x.UpdatedDate = null;
                    x.IsRecall = false;
                    x.RecallCode = null;
                });

                _soOrderRecallReqOrderRepo.AddRange(model.ListOrder);

                // Save all
                _soOrderRecallReqRepo.Save();

                if (!IsODSiteConstant && model.Status == SORECALLSTATUS.RELEASED)
                {
                    BaseResultModel syncDataToOD = await SyncDataToOD(model, token);
                    if (!syncDataToOD.IsSuccess) return syncDataToOD;
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Message = "Successfully",
                    Code = 200
                };
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

        public async Task<BaseResultModel> ConfirmOrderRequest(string code, string username, string token)
        {
            try
            {
                ResultModelWithObject<SORecallReqModel> resultDetail = await GetDetailReq(code);

                if (!resultDetail.IsSuccess)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = resultDetail.Code,
                        Message = resultDetail.Message
                    };
                }

                SORecallReqModel dataInDb = resultDetail.Data;

                if (dataInDb.Status == SORECALLSTATUS.RELEASED)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "Cannot confirm order has been released"
                    };
                }

                if (dataInDb.ListGiveBack.Count == 0)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "Cannot found product in list product give back",
                        Code = 404
                    };
                }

                if (!dataInDb.ListGiveBack.Any(x => x.IsDefault == true))
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "Cannot found product default in list product give back",
                        Code = 400
                    };
                }

                dataInDb.Status = SORECALLSTATUS.RELEASED;
                dataInDb.UpdatedBy = username;
                dataInDb.UpdatedDate = DateTime.Now;
                _soOrderRecallReqRepo.Update(dataInDb);

                if (!IsODSiteConstant && dataInDb.Status == SORECALLSTATUS.RELEASED)
                {
                    BaseResultModel syncDataToOD = await SyncDataToOD(dataInDb, token);
                    if (!syncDataToOD.IsSuccess) return syncDataToOD;
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Message = "Successfully",
                    Code = 200,
                    Data = (await GetDetailReq(code)).Data
                };
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

        public async Task<BaseResultModel> SyncDataToOD(SORecallReqModel model, string token)
        {
            try
            {
                if (!IsODSiteConstant && LinkODSystem != null)
                {
                    model.OwnerCode = _principleCode;
                    string _url = SystemUrl
                            .Where(x => x.Code == SystemUrlCode.ODSaleOrderAPI)
                            .Select(x => x.Url)
                            .FirstOrDefault();

                    if (_url == null)
                    {
                        return new BaseResultModel
                        {
                            Code = 404,
                            Message = "Cannot found api url ODSalesOrder",
                            IsSuccess = false,
                        };
                    }

                    _url = UrlHelperService.ExternalBaseUrl(_url, LinkODSystem);
                    //Handle Token
                    string tokenSplit = token.Split(" ").Last();
                    RestClient client = new(_url);
                    client.Authenticator = new JwtAuthenticator($"Rdos {tokenSplit}");
                    var request = new RestRequest($"/SORecall/Sync/CreateOrderRequest", Method.POST, DataFormat.Json);
                    request.AddJsonBody(model);
                    var resultData = await client.ExecuteAsync(request);
                    if (resultData == null || resultData.Content == String.Empty)
                    {
                        StagingSyncDataHistoryModel dataLogHis = new StagingSyncDataHistoryModel
                        {
                            Id = Guid.NewGuid(),
                            DataType = DataType.SORECALLREQ_SYNC,
                            RequestType = RequestType.INSERT,
                            InsertStatus = HistoryStatus.FAILED,
                            TimeRunAdhoc = DateTime.Now,
                            StartDate = DateTime.Now,
                            EndDate = DateTime.Now,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = DateTime.Now,
                            UpdatedBy = model.CreatedBy,
                            UpdatedDate = DateTime.Now,
                            ErrorMessage = "API OD SaleOrder is not working",
                            RollbackId = model.Id
                        };
                        await _commonService.SaveLogSync(dataLogHis);
                        return new BaseResultModel
                        {
                            IsSuccess = false,
                            Message = "Recall Order Request synchronization failed due to OD api not working",
                            Code = 500
                        };
                    }
                    else
                    {
                        var resultDataConvert = JsonConvert.DeserializeObject<BaseResultModel>(JsonConvert.DeserializeObject(resultData.Content).ToString());

                        StagingSyncDataHistoryModel dataLogHis = new StagingSyncDataHistoryModel
                        {
                            Id = Guid.NewGuid(),
                            DataType = DataType.SORECALLREQ_SYNC,
                            RequestType = RequestType.INSERT,
                            InsertStatus = resultDataConvert.IsSuccess ? HistoryStatus.SUCCESS : HistoryStatus.FAILED,
                            TimeRunAdhoc = DateTime.Now,
                            StartDate = DateTime.Now,
                            EndDate = DateTime.Now,
                            CreatedBy = model.CreatedBy,
                            CreatedDate = DateTime.Now,
                            UpdatedBy = model.CreatedBy,
                            UpdatedDate = DateTime.Now,
                            ErrorMessage = resultDataConvert.Message,
                            RollbackId = model.Id
                        };
                        await _commonService.SaveLogSync(dataLogHis);
                    }
                }


                return new BaseResultModel
                {
                    IsSuccess = true,
                    Message = "Successfully",
                    Code = 200
                };
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

        public async Task<ResultModelWithObject<SORecallReqModel>> GetDetailReq(string code)
        {
            try
            {
                SoorderRecallReq headerInDb = await _soOrderRecallReqRepo.GetAllQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.Code == code && !x.IsDeleted);

                if (headerInDb == null)
                {
                    return new ResultModelWithObject<SORecallReqModel>
                    {
                        IsSuccess = false,
                        Message = "Cannot found",
                        Code = 404,
                    };
                }

                SORecallReqModel dataRes = _mapper.Map<SORecallReqModel>(headerInDb);

                List<SoorderRecallReqScope> listScopeInDb = await _soOrderRecallReqScopeRepo.GetAllQueryable(x => x.RecallReqCode == code).AsNoTracking().ToListAsync();
                List<SoorderRecallReqOrder> listOrderInDb = await _soOrderRecallReqOrderRepo.GetAllQueryable(x => x.RecallReqCode == code).AsNoTracking().ToListAsync();
                dataRes.ListScope = listScopeInDb;
                dataRes.ListOrder = listOrderInDb;


                List<SoorderRecallReqGiveBack> listGiveBackInDb = await _soOrderRecallReqGiveBackRepo.GetAllQueryable(x => x.RecallReqCode == code).AsNoTracking().ToListAsync();
                dataRes.ListGiveBack = listGiveBackInDb;

                return new ResultModelWithObject<SORecallReqModel>
                {
                    IsSuccess = true,
                    Message = "Successfully",
                    Data = dataRes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<SORecallReqModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<ResultModelWithObject<SORecallReqListModel>> SearchReq(SORecallReqSearch parameters)
        {
            try
            {
                //search with pagination
                if (parameters.PageNumber <= 0) parameters.PageNumber = 1;
                var listDataInDb = _soOrderRecallReqRepo.GetAllQueryable()
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .AsQueryable();


                //if has filter expression 
                if (parameters.Filter != null && parameters.Filter.Trim() != string.Empty && parameters.Filter.Trim() != "NA_EMPTY")
                {
                    var parameter = Expression.Parameter(typeof(SoorderRecallReq), "s");
                    var lambda = DynamicExpressionParser.ParseLambda(new[] { parameter }, typeof(bool), parameters.Filter);
                    listDataInDb = listDataInDb.Where((Func<SoorderRecallReq, bool>)lambda.Compile()).AsQueryable();
                }


                //if search field
                if (!string.IsNullOrWhiteSpace(parameters.SearchValue))
                {
                    listDataInDb = listDataInDb.Where(x =>
                        (!string.IsNullOrEmpty(x.Code) && x.Code.ToLower().Contains(parameters.SearchValue.ToLower()))
                        || (!string.IsNullOrEmpty(x.Reason) && x.Reason.ToLower().Contains(parameters.SearchValue.ToLower())));
                }

                //filter created date
                if (parameters.FromDate.HasValue)
                {
                    listDataInDb = listDataInDb.Where(x => x.CreatedDate >= parameters.FromDate.Value.Date);
                }
                if (parameters.ToDate.HasValue)
                {
                    listDataInDb = listDataInDb.Where(x => x.CreatedDate <= parameters.ToDate.Value.Date.AddDays(1).AddTicks(-1));
                }

                //order by columns name
                if (parameters.OrderBy != null && parameters.OrderBy.Trim() != string.Empty && parameters.OrderBy.Trim() != "NA_EMPTY")
                {
                    listDataInDb = listDataInDb.OrderBy(parameters.OrderBy);
                }

                // if get list for dropdown
                if (parameters.IsDropdown)
                {
                    var page1 = PagedList<SoorderRecallReq>.ToPagedList(listDataInDb.ToList(), 0, listDataInDb.Count());

                    var reponse = new SORecallReqListModel { Items = page1.OrderByDescending(x => x.CreatedDate).ToList() };
                    return new ResultModelWithObject<SORecallReqListModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = reponse
                    };
                }

                var listTempPagged = PagedList<SoorderRecallReq>.ToPagedListQueryAble(listDataInDb, (parameters.PageNumber - 1) * parameters.PageSize, parameters.PageSize);
                var repsonse = new SORecallReqListModel { Items = listTempPagged.OrderByDescending(x => x.CreatedDate).ToList(), MetaData = listTempPagged.MetaData };

                //return metadata
                return new ResultModelWithObject<SORecallReqListModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = repsonse
                };
            }
            catch (Exception ex)
            {
                return new ResultModelWithObject<SORecallReqListModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public async Task<BaseResultModel> DeleteReq(string code, string username)
        {
            try
            {
                SoorderRecallReq headerInDb = await _soOrderRecallReqRepo.GetAllQueryable().FirstOrDefaultAsync(x => x.Code == code && !x.IsDeleted);

                if (headerInDb == null)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "Cannot found",
                        Code = 404
                    };
                }

                if (headerInDb.Status == SORECALLSTATUS.RELEASED)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "Cannot delete order has been released",
                        Code = 400
                    };
                }

                headerInDb.IsDeleted = true;
                headerInDb.UpdatedDate = DateTime.Now;
                headerInDb.UpdatedBy = username;
                _soOrderRecallReqRepo.Update(headerInDb);

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Message = "Successfully",
                    Code = 200
                };
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

        public async Task<BaseResultModel> GetDetailRecallReqForRecall(GetDetailRecallReqForRecallModel parameters)
        {
            try
            {
                parameters.DistributorCode = OD_Constant.DistributorCode;

                SoorderRecallReq headerInDb = await _soOrderRecallReqRepo
                    .GetAllQueryable(x => x.Code == parameters.RecallReqCode)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (headerInDb == null)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "Cannot found",
                        Code = 404
                    };
                }

                SORecallReqModel dataRes = _mapper.Map<SORecallReqModel>(headerInDb);

                List<SoorderRecallReqOrder> listOrder = await _soOrderRecallReqOrderRepo
                    .GetAllQueryable(x => x.DistributorCode == parameters.DistributorCode &&
                                    x.RecallReqCode == parameters.RecallReqCode &&
                                    x.IsRecall.HasValue && !x.IsRecall.Value)
                    .AsNoTracking()
                    .ToListAsync();

                dataRes.ListOrder = listOrder;

                List<SoorderRecallReqGiveBack> listGiveBack = await _soOrderRecallReqGiveBackRepo
                    .GetAllQueryable(x => x.RecallReqCode == parameters.RecallReqCode)
                    .AsNoTracking()
                    .ToListAsync();

                dataRes.ListGiveBack = listGiveBack;

                //return metadata
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = dataRes
                };
            }
            catch (Exception ex)
            {
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }
        #endregion
    }
}
