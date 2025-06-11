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
using SysAdmin.Models.StaticValue;
using Microsoft.AspNetCore.Http;

namespace ODSaleOrder.API.Services.SORecallService
{
    public class SORecallService : ISORecallService
    {
        // Service
        private readonly ILogger<SORecallReqService> _logger;
        private readonly IMapper _mapper;

        private readonly IDynamicBaseRepository<SoorderRecallReq> _soOrderRecallReqRepo;
        private readonly IDynamicBaseRepository<SoorderRecallReqScope> _soOrderRecallReqScopeRepo;
        private readonly IDynamicBaseRepository<SoorderRecallReqGiveBack> _soOrderRecallReqGiveBackRepo;
        private readonly IDynamicBaseRepository<SoorderRecallReqOrder> _soOrderRecallReqOrderRepo;
        private readonly IDynamicBaseRepository<SoorderRecall> _soOrderRecallRepo;
        private readonly IDynamicBaseRepository<SoorderRecallOrder> _soOrderRecallOrderRepo;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;
        private string _distributorCode = null;

        private readonly RDOSContext _db;

        public SORecallService(
            IHttpContextAccessor httpContextAccessor,
            ILogger<SORecallReqService> logger,
            IMapper mapper,
            RDOSContext db
        )
        {
            _db = db;
            _logger = logger;
            _soOrderRecallReqRepo = new DynamicBaseRepository<SoorderRecallReq>(_db);
            _soOrderRecallReqScopeRepo = new DynamicBaseRepository<SoorderRecallReqScope>(_db);
            _soOrderRecallReqGiveBackRepo = new DynamicBaseRepository<SoorderRecallReqGiveBack>(_db);
            _soOrderRecallReqOrderRepo = new DynamicBaseRepository<SoorderRecallReqOrder>(_db);
            _soOrderRecallRepo = new DynamicBaseRepository<SoorderRecall>(_db);
            _soOrderRecallOrderRepo = new DynamicBaseRepository<SoorderRecallOrder>(_db);
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
            _distributorCode = _httpContextAccessor.HttpContext?.Items["DistributorCode"] as string ?? _distributorCode;
        }
        #region Recall
        public async Task<BaseResultModel> ValidateOrderRecallCommon(SORecallModel model, SoorderRecallReq recallReqInDb = null)
        {
            try
            {
                if (model.RecallType != SORECALLFROMTYPE.DISTRIBUTOR &&
                    model.RecallType != SORECALLFROMTYPE.PRINCIPAL)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "RecallType code is incorrect",
                        Code = 400
                    };
                }

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

                if (model.Status == SORECALLSTATUS.RELEASED)
                {
                    if (model.ListOrder.Count == 0)
                    {
                        return new BaseResultModel
                        {
                            IsSuccess = false,
                            Message = "List order cannot null",
                            Code = 400
                        };
                    }
                }

                if (recallReqInDb != null)
                {
                    if (recallReqInDb.Status != SORECALLSTATUS.RELEASED)
                    {
                        return new BaseResultModel
                        {
                            IsSuccess = false,
                            Message = "Order recall request must have a status of RELEASED",
                            Code = 400
                        };
                    }
                }

                if (model.ListOrder != null && model.ListOrder.Count > 0 && recallReqInDb != null)
                {
                    foreach (var itemRecall in model.ListOrder)
                    {
                        var checkItem = await _soOrderRecallReqOrderRepo
                            .GetAllQueryable(x => 
                                x.RecallReqCode == recallReqInDb.Code && 
                                x.Id == itemRecall.RefDetailReqId)
                            .FirstOrDefaultAsync();

                        if (checkItem == null)
                        {
                            return new BaseResultModel
                            {
                                IsSuccess = false,
                                Message = "Cannot found order in list order of order recall request",
                                Code = 404
                            };
                        }

                        if (checkItem.IsRecall.HasValue && checkItem.IsRecall.Value)
                        {
                            return new BaseResultModel
                            {
                                IsSuccess = false,
                                Message = "Order in list order has been recalled",
                                Code = 404
                            };
                        }

                    }

                    foreach (var itemGivBack in model.ListOrder.GroupBy(x => x.ItemGiveBackCode).Select(x => x.First()).ToList()) 
                    {
                        var checkItemGiveBack = await _soOrderRecallReqGiveBackRepo
                            .GetAllQueryable(x =>
                                x.RecallReqCode == recallReqInDb.Code &&
                                x.ItemCode == itemGivBack.ItemGiveBackCode)
                            .FirstOrDefaultAsync();

                        if (checkItemGiveBack == null)
                        {
                            return new BaseResultModel
                            {
                                IsSuccess = false,
                                Message = "Cannot found item give back in list give back of order recall request",
                                Code = 404
                            };
                        }
                    }
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
        public async Task<BaseResultModel> InsertOrderRecall(SORecallModel model, string username, string token)
        {
            try
            {
                SoorderRecallReq recallReqInDb = null;
                if (!string.IsNullOrWhiteSpace(model.RequestRecallCode))
                {
                    recallReqInDb = await _soOrderRecallReqRepo
                        .GetAllQueryable()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Code == model.RequestRecallCode);

                    if (recallReqInDb == null)
                    {
                        return new BaseResultModel
                        {
                            IsSuccess = false,
                            Message = "Cannot found order recall requesst",
                            Code = 404
                        };
                    }
                }

                // Validate
                BaseResultModel validateResult = await ValidateOrderRecallCommon(model, recallReqInDb);
                if (!validateResult.IsSuccess) return validateResult;

                //Generate RefNumber
                var prefix = StringsHelper.GetPrefixYYM();
                var refNumber = await _soOrderRecallRepo.GetAllQueryable(null, null, null, _schemaName)
                    .Where(x => x.Code.Contains(prefix)).AsNoTracking().Select(x => x.Code).OrderByDescending(x => x).FirstOrDefaultAsync();
                var generatedNumber = StringsHelper.GennerateCodeWithYearMonthFormat(prefix, refNumber != null ? refNumber : null);

                //string _ownerType = OwnerTypeConstant.SYSTEM;
                //string _ownerCode = null;

                string _ownerType = OwnerTypeConstant.DISTRIBUTOR;
                string _ownerCode = _distributorCode;


                model.Id = Guid.NewGuid();
                model.Code = generatedNumber;
                model.CreatedBy = username;
                model.CreatedDate = DateTime.Now;
                model.UpdatedBy = null;
                model.UpdatedDate = null;
                model.OwnerType = _ownerType;
                model.OwnerCode = _ownerCode;
                model.IsDeleted = false;
                model.RequestRecallReason = recallReqInDb != null ? recallReqInDb.Reason : model.RequestRecallReason;

                _soOrderRecallRepo.Add(model, _schemaName);

                model.ListOrder.ForEach(x =>
                {
                    x.Id = Guid.NewGuid();
                    x.CreatedBy = username;
                    x.CreatedDate = DateTime.Now;
                    x.OwnerType = _ownerType;
                    x.OwnerCode = _ownerCode;
                    x.IsDeleted = false;
                    x.UpdatedBy = null;
                    x.UpdatedDate = null;
                    x.RecallCode = model.Code;
                });

                _soOrderRecallOrderRepo.AddRange(model.ListOrder, _schemaName);

                if (model.Status == SORECALLSTATUS.RELEASED)
                {
                    BaseResultModel bookInvResult = await BookInventory(model, token, username);
                    if (!bookInvResult.IsSuccess) return bookInvResult;
                }

                _soOrderRecallRepo.Save(_schemaName);

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Message = "Successfully",
                    Code = 201,
                    Data = (await GetDetail(generatedNumber)).Data
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

        public async Task<BaseResultModel> UpdateOrderRecall(SORecallModel model, string username, string token)
        {
            try
            {
                // Check data update
                SoorderRecall dataInDb = await _soOrderRecallRepo
                    .GetAllQueryable(null, null, null, _schemaName)
                    .FirstOrDefaultAsync(x => x.Code == model.Code && !x.IsDeleted);

                if (dataInDb == null)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 404,
                        Message = "Cannot found order recall"
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

                SoorderRecallReq recallReqInDb = null;
                if (!string.IsNullOrWhiteSpace(model.RequestRecallCode))
                {
                    recallReqInDb = await _soOrderRecallReqRepo
                        .GetAllQueryable()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Code == model.RequestRecallCode);

                    if (recallReqInDb == null)
                    {
                        return new BaseResultModel
                        {
                            IsSuccess = false,
                            Message = "Cannot found order recall requesst",
                            Code = 404
                        };
                    }
                }

                // Validate
                BaseResultModel validateResult = await ValidateOrderRecallCommon(model, recallReqInDb);
                if (!validateResult.IsSuccess) return validateResult;

                // Keep data no change
                string createdBy = dataInDb.CreatedBy;
                DateTime? createdDate = dataInDb.CreatedDate;
                string ownerType = dataInDb.OwnerType;
                string ownerCode = dataInDb.OwnerCode;
                Guid idTemp = dataInDb.Id;

                // Auto map data request to data indb
                _mapper.Map(model, dataInDb);

                dataInDb.Id = idTemp;
                dataInDb.CreatedDate = createdDate;
                dataInDb.OwnerType = ownerType;
                dataInDb.OwnerCode = ownerCode;
                dataInDb.CreatedBy = createdBy;
                dataInDb.UpdatedBy = username;
                dataInDb.UpdatedDate = DateTime.Now;
                dataInDb.RequestRecallReason = recallReqInDb != null ? recallReqInDb.Reason : model.RequestRecallReason;

                _soOrderRecallRepo.UpdateUnSaved(dataInDb, _schemaName);

                // List order
                List<SoorderRecallOrder> soOrderRecallOrderInDbs = await _soOrderRecallOrderRepo
                    .GetAllQueryable(x => 
                        x.RecallCode == dataInDb.Code, 
                        null, null, _schemaName)
                    .ToListAsync();

                if (soOrderRecallOrderInDbs.Count > 0)
                {
                    _soOrderRecallOrderRepo.RemoveRange(soOrderRecallOrderInDbs, _schemaName);
                }

                model.ListOrder.ForEach(x =>
                {
                    x.Id = Guid.NewGuid();
                    x.CreatedBy = username;
                    x.CreatedDate = DateTime.Now;
                    x.OwnerType = dataInDb.OwnerType;
                    x.OwnerCode = dataInDb.OwnerCode;
                    x.IsDeleted = false;
                    x.UpdatedBy = null;
                    x.UpdatedDate = null;
                    x.RecallCode = dataInDb.Code;
                });

                _soOrderRecallOrderRepo.AddRange(model.ListOrder, _schemaName);

                if (model.Status == SORECALLSTATUS.RELEASED)
                {
                    BaseResultModel bookInvResult = await BookInventory(model, token, username);
                    if (!bookInvResult.IsSuccess) return bookInvResult;
                }

                _soOrderRecallRepo.Save(_schemaName);
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

        public async Task<ResultModelWithObject<SORecallModel>> GetDetail(string code)
        {
            try
            {
                SoorderRecall headerInDb = await _soOrderRecallRepo
                    .GetAllQueryable(null, null, null, _schemaName)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Code == code && !x.IsDeleted);

                if (headerInDb == null)
                {
                    return new ResultModelWithObject<SORecallModel>
                    {
                        IsSuccess = false,
                        Message = "Cannot found",
                        Code = 404
                    };
                }

                SORecallModel dataRes = _mapper.Map<SORecallModel>(headerInDb);

                List<SoorderRecallOrder> listOrderInDb = await _soOrderRecallOrderRepo.GetAllQueryable(x => x.RecallCode == code, null, null, _schemaName).AsNoTracking().ToListAsync();
                dataRes.ListOrder = listOrderInDb;


                return new ResultModelWithObject<SORecallModel>
                {
                    IsSuccess = true,
                    Message = "Successfully",
                    Data = dataRes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<SORecallModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<ResultModelWithObject<SORecallListModel>> Search(SORecallSearch parameters)
        {
            try
            {
                //search with pagination
                if (parameters.PageNumber <= 0) parameters.PageNumber = 1;
                var listDataInDb = _soOrderRecallRepo
                    .GetAllQueryable(null, null, null, _schemaName)
                    .AsNoTracking()
                    .Where(x => !x.IsDeleted)
                    .AsQueryable();


                //if has filter expression 
                if (parameters.Filter != null && parameters.Filter.Trim() != string.Empty && parameters.Filter.Trim() != "NA_EMPTY")
                {
                    var parameter = Expression.Parameter(typeof(SoorderRecall), "s");
                    var lambda = DynamicExpressionParser.ParseLambda(new[] { parameter }, typeof(bool), parameters.Filter);
                    listDataInDb = listDataInDb.Where((Func<SoorderRecall, bool>)lambda.Compile()).AsQueryable();
                }


                //if search field
                if (!string.IsNullOrWhiteSpace(parameters.SearchValue))
                {
                    listDataInDb = listDataInDb.Where(x =>
                        (!string.IsNullOrEmpty(x.Code) && x.Code.ToLower().Contains(parameters.SearchValue.ToLower()))
                        || (!string.IsNullOrEmpty(x.Description) && x.Description.ToLower().Contains(parameters.SearchValue.ToLower())));
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
                    var page1 = PagedList<SoorderRecall>.ToPagedList(listDataInDb.ToList(), 0, listDataInDb.Count());

                    var reponse = new SORecallListModel { Items = page1.OrderByDescending(x => x.CreatedDate).ToList() };
                    return new ResultModelWithObject<SORecallListModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = reponse
                    };
                }

                var listTempPagged = PagedList<SoorderRecall>.ToPagedListQueryAble(listDataInDb, (parameters.PageNumber - 1) * parameters.PageSize, parameters.PageSize);
                var repsonse = new SORecallListModel { Items = listTempPagged.OrderByDescending(x => x.CreatedDate).ToList(), MetaData = listTempPagged.MetaData };

                //return metadata
                return new ResultModelWithObject<SORecallListModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = repsonse
                };
            }
            catch (Exception ex)
            {
                return new ResultModelWithObject<SORecallListModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        public async Task<BaseResultModel> Delete(string code, string username)
        {
            try
            {
                SoorderRecall headerInDb = await _soOrderRecallRepo
                    .GetAllQueryable(null, null, null, _schemaName)
                    .FirstOrDefaultAsync(x => x.Code == code && !x.IsDeleted);

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
                _soOrderRecallRepo.Update(headerInDb, _schemaName);

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

        public async Task<BaseResultModel> ConfirmOrder(string code, string username, string token)
        {
            try
            {
                ResultModelWithObject<SORecallModel> resultDetail = await GetDetail(code);

                if (!resultDetail.IsSuccess)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = resultDetail.Code,
                        Message = resultDetail.Message
                    };
                }

                SORecallModel dataInDb = resultDetail.Data;

                if (dataInDb.Status == SORECALLSTATUS.RELEASED)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "Cannot confirm order has been released"
                    };
                }

                if (dataInDb.ListOrder.Count == 0)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = "Cannot found order in list order",
                        Code = 404
                    };
                }

                dataInDb.Status = SORECALLSTATUS.RELEASED;
                dataInDb.UpdatedBy = username;
                dataInDb.UpdatedDate = DateTime.Now;

                BaseResultModel bookInvResult = await BookInventory(dataInDb, token, username);
                if (!bookInvResult.IsSuccess) return bookInvResult;

                _soOrderRecallRepo.Update(dataInDb, _schemaName);
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Message = "Successfully",
                    Code = 200,
                    Data = (await GetDetail(code)).Data
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

        public async Task<BaseResultModel> BookInventory(SORecallModel model, string token, string username)
        {
            try
            {
                List<INV_TransactionModel> transactionData = new();

                foreach (var item in model.ListOrder)
                {
                    // Transaction recall
                    transactionData.Add(new INV_TransactionModel
                    {
                        OrderCode = model.Code,
                        ItemId = Guid.Empty,
                        ItemCode = item.ItemCode,
                        ItemDescription = item.ItemDescription,
                        Uom = item.Uom,
                        Quantity = item.RecallQty,
                        BaseQuantity = item.RecallBaseQty,
                        OrderBaseQuantity = item.RecallBaseQty,
                        TransactionDate = DateTime.Now,
                        TransactionType = INV_TransactionType.SO_RECALL,
                        WareHouseCode = model.DistributorShiptoCode,
                        LocationCode = model.RecallLocationCode,
                        DistributorCode = item.DistributorCode == null ? _distributorCode : item.DistributorCode,
                        DSACode = null,
                        Description = model.Description
                    });

                    // Transaction give back
                    transactionData.Add(new INV_TransactionModel
                    {
                        OrderCode = model.Code,
                        ItemId = Guid.Empty,
                        ItemCode = item.ItemGiveBackCode,
                        ItemDescription = item.ItemGiveBackDesc,
                        Uom = item.GiveBackUom,
                        Quantity = item.GivBackQty,
                        BaseQuantity = item.GiveBackBaseQty,
                        OrderBaseQuantity = item.GiveBackBaseQty,
                        TransactionDate = DateTime.Now,
                        TransactionType = INV_TransactionType.SO_GIVEBACK,
                        WareHouseCode = model.DistributorShiptoCode,
                        LocationCode = model.GiveBackLocationCode,
                        DistributorCode = item.DistributorCode == null ? _distributorCode : item.DistributorCode,
                        DSACode = null,
                        Description = model.Description
                    });

                    if (item.RefDetailReqId != null && item.RefDetailReqId != Guid.Empty)
                    {
                        SoorderRecallReqOrder orderRecallReqInDb = await _soOrderRecallReqOrderRepo
                            .GetAllQueryable(x => x.Id == item.RefDetailReqId.Value)
                            .FirstOrDefaultAsync();

                        orderRecallReqInDb.IsRecall = true;
                        orderRecallReqInDb.RecallCode = item.RecallCode;
                        orderRecallReqInDb.UpdatedBy = username;
                        orderRecallReqInDb.UpdatedDate = DateTime.Now;
                        _soOrderRecallReqOrderRepo.UpdateUnSaved(orderRecallReqInDb);
                    }
                }

                //call api transaction
                var client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.ODInventoryAPI).Select(x => x.Url).FirstOrDefault());
                // client = new RestClient("http://localhost:47016/api/v1");
                client.Authenticator = new JwtAuthenticator($"{token}");
                var json = JsonConvert.SerializeObject(transactionData);
                RestRequest request = new RestRequest($"InventoryTransaction/BulkCreate", Method.POST);
                request.AddJsonBody(json);
                // Add Header
                request.AddHeader(OD_Constant.KeyHeader, _distributorCode);
                var result = client.Execute(request);

                var resultData = JsonConvert.DeserializeObject<BaseResultModel>(JsonConvert.DeserializeObject(result.Content).ToString());
                if (!resultData.IsSuccess)
                {
                    resultData.Message = "Inventory transaction: " + resultData.Message;
                    resultData.Data = null;
                    return resultData;
                }

                _soOrderRecallReqOrderRepo.Save();
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully"
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
        #endregion
    }
}
