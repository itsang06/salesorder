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
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;
using SysAdmin.Models.StaticValue;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using DynamicSchema.Helper.Services.Interface;
using DynamicSchema.Helper.Services;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;

namespace ODSaleOrder.API.Services.SaleOrder
{
    public class SaleOrderReturnService : ISaleOrderReturnService
    {
        private readonly ILogger<SaleOrderReturnService> _logger;
        private readonly IMapper _mapper;
        private readonly IDynamicBaseRepository<SO_FirstTimeCustomer> _firstTimeCustomerRepository;
        private readonly IDynamicBaseRepository<SO_OrderInformations> _orderInformationsRepository;
        private readonly IDynamicBaseRepository<SO_OrderItems> _orderItemsRepository;
        private readonly IDynamicBaseRepository<SO_Reason> _reasonRepository;
        private readonly IDynamicBaseRepository<ProgramCustomersDetail> _customerProgramDetailRepo;
        private readonly IDynamicBaseRepository<ProgramCustomers> _customerProgramRepo;
        private readonly IDynamicBaseRepository<Principal> _principalRepo;
        private readonly IDynamicBaseRepository<SO_SalesOrderSetting> _settingRepository;

        public readonly IClientService _clientService;
        public IRestClient _client;
        public readonly ISalesOrderService _soService;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;
        private string _distributorCode = null;

        public SaleOrderReturnService(ILogger<SaleOrderReturnService> logger,
            IMapper mapper,
            RDOSContext dataContext,
            IClientService clientService,
            ISalesOrderService soService,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _mapper = mapper;
            _firstTimeCustomerRepository = new DynamicBaseRepository<SO_FirstTimeCustomer>(dataContext);
            _orderInformationsRepository = new DynamicBaseRepository<SO_OrderInformations>(dataContext);
            _reasonRepository = new DynamicBaseRepository<SO_Reason>(dataContext);
            _orderItemsRepository = new DynamicBaseRepository<SO_OrderItems>(dataContext);
            _customerProgramDetailRepo = new DynamicBaseRepository<ProgramCustomersDetail>(dataContext);
            _customerProgramRepo = new DynamicBaseRepository<ProgramCustomers>(dataContext);
            _principalRepo = new DynamicBaseRepository<Principal>(dataContext);
            _soService = soService;
            _clientService = clientService;
            _settingRepository = new DynamicBaseRepository<SO_SalesOrderSetting>(dataContext);

            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
            _distributorCode = _httpContextAccessor.HttpContext?.Items["DistributorCode"] as string ?? _distributorCode;
        }

        #region Common function
        public async Task<List<SaleOrderBaseModel>> CommonGetDetail(SaleOrderDetailQueryModel query)
        {
            try
            {
                var baseModel = await (from header in _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking()
                                       join detail in _orderItemsRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking() on header.OrderRefNumber equals detail.OrderRefNumber into data
                                       from detail in data.DefaultIfEmpty()
                                       where header.OrderRefNumber == query.OrderRefNumber && header.DistributorCode == query.DistributorCode && !header.IsDeleted && (!string.IsNullOrWhiteSpace(header.OrderType) && header.OrderType == SO_SaleOrderTypeConst.ReturnOrder)
                                       select new SaleOrderBaseModel
                                       {
                                           OrderInformation = header,
                                           OrderItem = detail
                                       }).AsNoTracking().AsSplitQuery().ToListAsync();
                return baseModel;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new List<SaleOrderBaseModel>();
            }
        }

        public async Task<IQueryable<SO_OrderInformations>> CommonGetAllQueryable(SaleOrderSearchParamsModel parameters)
        {
            try
            {
                //var query = (from header in _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking()
                //             join detail in _orderItemsRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking() on header.OrderRefNumber equals detail.OrderRefNumber into data
                //             from detail in data.DefaultIfEmpty()
                //             where (parameters.OrderRefNumbers != null || parameters.OrderRefNumbers.Count > 0 ? parameters.OrderRefNumbers.Contains(header.OrderRefNumber) : true) && header.DistributorCode == parameters.DistributorCode && !header.IsDeleted && header.isReturn
                //             select new SaleOrderBaseModel
                //             {
                //                 OrderInformation = header,
                //                 OrderItem = detail
                //             }).AsNoTracking().AsSplitQuery();

                var query = _orderInformationsRepository.GetAllQueryable(x =>
                    (parameters.OrderRefNumbers != null || parameters.OrderRefNumbers.Count > 0 ? parameters.OrderRefNumbers.Contains(x.OrderRefNumber) : true) &&
                    x.DistributorCode == parameters.DistributorCode && !x.IsDeleted && x.isReturn, null, null, _schemaName).AsNoTracking();

                //if has filter expression 
                if (parameters.Filter != null && parameters.Filter.Trim() != string.Empty && parameters.Filter.Trim() != "NA_EMPTY")
                {
                    //var optionsAssembly = ScriptOptions.Default.AddReferences(typeof(SaleOrderBaseModel).Assembly);
                    //var filterExpressionTemp = CSharpScript.EvaluateAsync<Func<SaleOrderBaseModel, bool>>(($"s=> {parameters.Filter}"), optionsAssembly);
                    //Func<SaleOrderBaseModel, bool> filterExpression = filterExpressionTemp.Result;
                    //var checkCondition = query.Where(filterExpression);
                    //res = checkCondition.ToList();

                    var parameter = Expression.Parameter(typeof(SO_OrderInformations), "s");
                    var lambda = DynamicExpressionParser.ParseLambda(new[] { parameter }, typeof(bool), parameters.Filter);
                    query = query.Where((Func<SO_OrderInformations, bool>)lambda.Compile()).AsQueryable();
                }


                //query = query.Where(x => x != null && x.OrderInformation != null && x.OrderItem != null);
                //if has filter expression

                //if (parameters.Filters != null && parameters.Filters.Count > 0)
                //{
                //    foreach (var filter in parameters.Filters)
                //    {
                //        var getter = typeof(SO_OrderInformations).GetProperty(filter.Property);
                //        result = result.Where(x => filter.Values.Any(a => a == "" || a == null) ?
                //                string.IsNullOrEmpty(getter.GetValue(x.OrderInformation, null).EmptyIfNull().ToString()) || filter.Values.Contains(getter.GetValue(x.OrderInformation, null).ToString().ToLower().Trim()) :
                //                !string.IsNullOrEmpty(getter.GetValue(x.OrderInformation, null).EmptyIfNull().ToString()) && filter.Values.Contains(getter.GetValue(x.OrderInformation, null).ToString().ToLower().Trim()));
                //    }
                //}

                //if has filter expression
                if (parameters.Filters != null && parameters.Filters.Count > 0)
                {
                    foreach (var filter in parameters.Filters)
                    {
                        var filterExpression = FilterGeneric<SO_OrderInformations>.BuildFilterExpression(filter);
                        query = query.Where(filterExpression).AsQueryable();
                    }
                }
                return query;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return null;
            }
        }

        public BaseResultModel CommonCalulateOrderHeader(ref SaleOrderModel model)
        {
            try
            {
                model.Orig_Ord_SKUs = 0;
                model.Ord_SKUs = 0;
                model.Shipped_SKUs = 0;
                model.Orig_Ord_Qty = 0;
                model.Ord_Qty = 0;
                model.Shipped_Qty = 0;
                model.Orig_Promotion_Qty = 0;
                model.Promotion_Qty = 0;
                model.Shipped_Promotion_Qty = 0;
                model.Orig_Ord_Amt = 0;
                model.Ord_Amt = 0;
                model.Shipped_Amt = 0;
                model.Orig_Ord_Disc_Amt = 0;
                model.Ord_Disc_Amt = 0;
                model.Shipped_Disc_Amt = 0;
                model.Orig_Ordline_Disc_Amt = 0;
                model.Ordline_Disc_Amt = 0;
                model.Shipped_line_Disc_Amt = 0;
                model.Orig_Ord_Extend_Amt = 0;
                model.Ord_Extend_Amt = 0;
                model.Shipped_Extend_Amt = 0;
                model.TotalVAT = 0;
                foreach (var item in model.OrderItems)
                {
                    model.Orig_Ord_SKUs += item.OriginalOrderQuantities > 0 ? 1 : 0; //Số SP ban đầu trên đơn hàng
                    model.Ord_SKUs += item.OrderQuantities > 0 ? 1 : 0; //Số SP trên đơn hàng được xác nhận
                    model.Shipped_SKUs += item.ShippedQuantities > 0 ? 1 : 0; //Số sản phẩm giao thành công
                    // model.Orig_Ord_Qty += item.OriginalOrderQuantities; //Tổng sản lượng đặt ban đầu trên đơn hàng
                    model.Ord_Qty += item.ReturnBaseQuantities; //Tổng sản lượng xác nhận đặt trên đơn hàng
                    model.Shipped_Qty += item.ShippedBaseQuantities; //Tổng sản lượng giao thành công trên đơn hàng
                    if (item.DiscountID != null)
                    {
                        model.Orig_Promotion_Qty += item.OriginalOrderQuantities; //Tổng sản lượng KM ban đầu trên đơn hàng
                        model.Promotion_Qty += item.OrderQuantities; //Tổng sản lượng KM được xác nhận trên đơn hàng
                        model.Shipped_Promotion_Qty += item.ShippedQuantities; //Tổng sản lượng KM được giao thành công trên dơn hàng
                    }
                    model.Orig_Ord_Amt += item.Orig_Ord_Line_Amt;//Tổng doanh số đặt ban đầu trên đơn hàng
                    model.Ord_Amt += item.Ord_Line_Amt;//Tổng doanh số được xác nhận trên đơn hàng
                    model.Shipped_Amt += item.Shipped_Line_Amt; //Tổng doanh số được giao thành công trên đơn hàng

                    model.Orig_Ord_Disc_Amt += item.Orig_Ord_line_Disc_Amt;  //Tổng tiền CK ban đầu trên ĐH
                    model.Ord_Disc_Amt += item.Ord_line_Disc_Amt;  //Tổng tiền CK trên ĐH được xác nhận
                    model.Shipped_Disc_Amt += item.Shipped_line_Disc_Amt;  //Tổng tiền CK giao thành công trên ĐH
                    model.Orig_Ordline_Disc_Amt += item.Orig_Ord_line_Disc_Amt;  //Tổng tiền KM ban đầu trên ĐH
                    model.Ordline_Disc_Amt += item.Ord_line_Disc_Amt;  //Tổng tiền KM trên ĐH được xác nhận
                    model.Shipped_line_Disc_Amt += item.Shipped_line_Disc_Amt;  //Tổng tiền KM giao thành công trên ĐH
                    model.Ord_Extend_Amt += item.Ord_Line_Extend_Amt;  //Tổng tiền sau CK và KM được xác nhận
                    model.Shipped_Extend_Amt += item.Shipped_Line_Extend_Amt;  //Tổng tiền sau CK và KM được giao thành công
                    model.TotalVAT += item.VAT;  //Tổng số thuế

                    item.Ord_Line_Extend_Amt = item.Ord_Line_Amt - item.Ord_line_Disc_Amt;
                }
                model.Orig_Ord_Extend_Amt = model.Orig_Ord_Amt - model.Orig_Ord_Disc_Amt;  //Tổng tiền sau CK và KM ban đầu
                model.Ord_Extend_Amt = model.Ord_Amt - model.Ord_Disc_Amt;  //Tổng tiền sau CK và KM ban đầu

                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
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

        public async Task<BaseResultModel> CommonConfirm(SaleOrderModel model, string username, string token)
        {
            try
            {
                List<INV_TransactionModel> transactionData = new();
                foreach (var item in model.OrderItems)
                {
                    transactionData.Add(new INV_TransactionModel
                    {
                        OrderCode = model.OrderRefNumber,
                        ItemId = item.ItemId,
                        ItemCode = item.ItemCode,
                        ItemDescription = item.ItemDescription,
                        Uom = item.UOM,
                        Quantity = item.ReturnQuantities, //Số lượng đặt
                        OrderBaseQuantity = item.ReturnBaseQuantities,//Số lượng base của thằng trên
                        BaseQuantity = item.ReturnBaseQuantities,  //Số lượng thực tế
                        TransactionDate = DateTime.Now,
                        TransactionType = INV_TransactionType.SO_RE,
                        WareHouseCode = model.WareHouseID,
                        LocationCode = item.LocationID,
                        DistributorCode = model.DistributorCode,
                        DSACode = model.DSAID,
                        Description = model.Note,
                        ReasonCode = model.ReasonCode,
                        ReasonDescription = model.ReasonCode != null ? (await _reasonRepository.GetAllQueryable(null, null, null, OD_Constant.DEFAULT_SCHEMA).AsNoTracking().FirstOrDefaultAsync(x => !string.IsNullOrWhiteSpace(x.ReasonCode) && x.ReasonCode == model.ReasonCode))?.Description ?? null : null
                    });
                }

                //call api transaction
                _client = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.ODInventoryAPI).Select(x => x.Url).FirstOrDefault());
                _client.Authenticator = new JwtAuthenticator($"{token}");
                var json = JsonConvert.SerializeObject(transactionData);
                var request = new RestRequest($"InventoryTransaction/BulkCreate", Method.POST);
                request.AddJsonBody(json);
                // Add Header
                request.AddHeader(OD_Constant.KeyHeader, _distributorCode);
                var result = _client.Execute(request);

                var resultData = JsonConvert.DeserializeObject<BaseResultModel>(JsonConvert.DeserializeObject(result.Content).ToString());
                if (!resultData.IsSuccess)
                {
                    resultData.Message = "Inventory transaction: " + resultData.Message;
                    return resultData;
                }

                if (model.ReferenceRefNbr != null)
                {
                    var referredSO = await _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName)
                        .FirstOrDefaultAsync(x => !x.IsDeleted && x.OrderRefNumber == model.ReferenceRefNbr);
                    if (referredSO != null)
                    {
                        referredSO.ReferenceRefNbr = model.OrderRefNumber;
                        if (model.Ord_Qty == model.Shipped_Qty)
                        {
                            referredSO.isReturn = true;
                        }
                        referredSO.UpdatedDate = DateTime.Now;
                        referredSO.UpdatedBy = username;
                        _orderInformationsRepository.UpdateUnSaved(referredSO, _schemaName);
                    }

                }

                // Trả Booked prômtion budget
                if (model.PromotionRefNumber != null)
                {
                    var promoCusDetails = await _customerProgramDetailRepo.GetAllQueryable(x =>
                        !string.IsNullOrEmpty(x.BudgetCode) &&
                        !string.IsNullOrEmpty(x.PromotionRefNumber) && x.PromotionRefNumber == model.PromotionRefNumber,
                        null, null, _schemaName).ToListAsync();

                    if (promoCusDetails != null && promoCusDetails.Count > 0)
                    {
                        foreach (var cusDetail in promoCusDetails)
                        {
                            if (cusDetail.BudgetBooked > 0)
                            {
                                var promo = await _customerProgramRepo.GetAllQueryable(x => x.ProgramCustomersKey == cusDetail.ProgramCustomersKey, null, null, _schemaName).FirstOrDefaultAsync();
                                var principal = await _principalRepo.GetAllQueryable(null, null, null, OD_Constant.DEFAULT_SCHEMA).FirstOrDefaultAsync();
                                var budgetDataReq = new BudgetReqModel
                                {
                                    budgetCode = cusDetail.BudgetCode,
                                    budgetType = cusDetail.BudgetType,
                                    customerCode = promo.CustomerCode,
                                    customerShipTo = promo.ShiptoCode,
                                    saleOrg = promo.SalesOrgCode,
                                    budgetAllocationLevel = cusDetail.BudgetAllocationLevel,
                                    budgetBook = -cusDetail.BudgetBooked,
                                    salesTerritoryValueCode = null,
                                    promotionCode = promo.ProgramCode,
                                    promotionLevel = cusDetail.DetailLevel,
                                    routeZoneCode = promo.RouteZoneCode,
                                    dsaCode = promo.DsaCode,
                                    subAreaCode = promo.SubArea,
                                    areaCode = promo.Area,
                                    subRegionCode = promo.SubRegion,
                                    regionCode = promo.Region,
                                    branchCode = promo.Branch,
                                    nationwideCode = principal.Country,
                                    salesOrgCode = promo.SalesOrgCode,
                                    referalCode = null, //?
                                    distributorCode = _distributorCode
                                };
                                switch (cusDetail.BudgetAllocationLevel)
                                {
                                    case "DSA":
                                        {
                                            budgetDataReq.salesTerritoryValueCode = promo.DsaCode;
                                            break;
                                        }
                                    case "TL01":
                                        {
                                            budgetDataReq.salesTerritoryValueCode = promo.Branch;
                                            break;
                                        }
                                    case "TL02":
                                        {
                                            budgetDataReq.salesTerritoryValueCode = promo.Region;
                                            break;
                                        }
                                    case "TL03":
                                        {
                                            budgetDataReq.salesTerritoryValueCode = promo.SubRegion;
                                            break;
                                        }
                                    case "TL04":
                                        {
                                            budgetDataReq.salesTerritoryValueCode = promo.Area;
                                            break;
                                        }
                                    case "TL05":
                                        {
                                            budgetDataReq.salesTerritoryValueCode = promo.SubArea;
                                            break;
                                        }
                                    default:
                                        break;
                                }

                                // Trả budget
                                var budgetChecked = (await _clientService.CommonRequestAsync<ResultModelWithObject<BudgetResModel>>(CommonData.SystemUrlCode.ODTpAPI, $"external_checkbudget/checkbudget", Method.POST, token, budgetDataReq)).Data;
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }
                }
                else
                {
                    foreach(var item in model.OrderItems.Where(x => !x.IsDeleted && !string.IsNullOrEmpty(x.PromotionBudgetCode) && x.PromotionBudgetQuantities > 0).ToList())
                    {
                        await _soService.HandleCancelBudgetSO(item, model, token);
                    }
                }

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
        #endregion

        public async Task<BaseResultModel> InsertOrder(SaleOrderModel model, string token, string username, bool includeConfirm = false)
        {
            try
            {
                model.DistributorCode = _distributorCode;
                model.OwnerCode = model.OwnerCode;
                model.OwnerType = model.OwnerType;
                //Generate RefNumber
                var prefix = StringsHelper.GetPrefixYYM();
                var orderRefNumberIndb = await _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName)
                    .Where(x => x.OrderRefNumber.Contains(prefix)).AsNoTracking().Select(x => x.OrderRefNumber).OrderByDescending(x => x).FirstOrDefaultAsync();
                var generatedNumber = StringsHelper.GennerateCodeWithYearMonthFormat(prefix, orderRefNumberIndb != null ? orderRefNumberIndb : null);

                bool checkExisted = false;
                do
                {
                    var settingInDb = await _settingRepository.GetAllQueryable(null, null, null, _schemaName).FirstOrDefaultAsync();
                    if (settingInDb != null && settingInDb.OrderRefNumber == generatedNumber)
                    {
                        checkExisted = false;
                        generatedNumber = String.Format("{0}{1:00000}", prefix, generatedNumber != null ? generatedNumber.Substring(3).TryParseInt() + 1 : 0);
                    }
                    else
                    {
                        checkExisted = true;
                        settingInDb.OrderRefNumber = generatedNumber;
                        settingInDb.UpdatedDate = DateTime.Now;
                        settingInDb.UpdatedBy = username;
                        _settingRepository.Update(settingInDb, _schemaName);
                    }
                } while (!checkExisted);

                var handlerAttResult = await _soService.CommonHandleInternalSoAttribute(model, token);
                if (handlerAttResult.IsSuccess)
                {
                    model = handlerAttResult.Data;
                }

                //enhance 20250324 update order item
                var soOld = _orderInformationsRepository.GetById(model.Id, _schemaName);
                soOld.isReturn = true;
                soOld.ReasonCode = model.ReasonCode;
                soOld.UpdatedDate = DateTime.Now;
                soOld.UpdatedBy = username;
                _orderInformationsRepository.Update(soOld, _schemaName);

                model.Id = Guid.NewGuid();
                model.CreatedBy = username;
                model.CreatedDate = DateTime.Now;
                model.OrderRefNumber = generatedNumber;
                model.OrderType = SO_SaleOrderTypeConst.ReturnOrder;
                model.isReturn = false;
                model.Source = SO_SOURCE_CONST.NOTMOBILE;
                model.ReasonCode = null;

                var calculateResult = CommonCalulateOrderHeader(ref model);

                var headerInsertData = _mapper.Map<SO_OrderInformations>(model);
                _orderInformationsRepository.Add(headerInsertData, _schemaName);

                foreach (var item in model.OrderItems)
                {
                    var itemInsertData = item;
                    item.Id = Guid.NewGuid();
                    item.CreatedBy = username;
                    item.CreatedDate = DateTime.Now;
                    item.OrderRefNumber = model.OrderRefNumber;
                    //if (IsODSiteConstant)
                    //{
                        item.OwnerCode = model.OwnerCode;
                        item.OwnerType = model.OwnerType;
                    //}
                    //enhance 20250324
                    item.ShippedQuantities = 0;
                    item.ShippedBaseQuantities = 0;
                    item.Shipped_Line_Extend_Amt = 0;
                    item.Shipped_Line_TaxBefore_Amt = 0;
                    item.Shipped_Line_TaxAfter_Amt = 0;

                    _orderItemsRepository.Add(itemInsertData, _schemaName);
                }

                if (includeConfirm)
                {
                    var confirmResult = await CommonConfirm(model, username, token);
                    if (!confirmResult.IsSuccess)
                    {
                        return confirmResult;
                    }
                }

                _orderInformationsRepository.Save(_schemaName);                

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

        public async Task<BaseResultModel> SaveWithConfirm(SaleOrderModel model, string token, string username)
        {
            try
            {
                if (model.Id == null || model.Id == Guid.Empty)
                {
                    return await InsertOrder(model, token, username, true);
                }
                else
                {
                    return await UpdateSOReturn(model, token, username, true);
                }

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

        public async Task<BaseResultModel> UpdateSOReturn(SaleOrderModel model, string token, string username, bool includeConfirm = false)
        {
            try
            {
                //if (IsODSiteConstant)
                //{
                    model.DistributorCode = _distributorCode;
                    model.OwnerCode = model.OwnerCode;
                    model.OwnerType = model.OwnerType;
                //}

                var baseModel = await CommonGetDetail(new SaleOrderDetailQueryModel { DistributorCode = model.DistributorCode, OrderRefNumber = model.OrderRefNumber });
                if (baseModel == null || baseModel.Count == 0)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "SaleOrder not found"
                    };
                }

                var calculateResult = CommonCalulateOrderHeader(ref model);

                var headerUpdateData = _mapper.Map<SO_OrderInformations>(model);
                if (baseModel.First().OrderInformation.Status == SO_SaleOrderStatusConst.CONFIRM || baseModel.First().OrderInformation.Status == SO_SaleOrderStatusConst.CANCEL)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "Cannot update comfirmed or canceled return order"
                    };
                }
                model.UpdatedBy = username;
                model.UpdatedDate = DateTime.Now;
                _orderInformationsRepository.UpdateUnSaved(headerUpdateData, _schemaName);

                foreach (var item in model.OrderItems)
                {
                    //if (IsODSiteConstant)
                    //{
                        item.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                        item.OwnerCode = _distributorCode;
                    //}

                    if (item.Id != Guid.Empty)
                    {
                        item.UpdatedBy = username;
                        item.UpdatedDate = DateTime.Now;
                        _orderItemsRepository.UpdateUnSaved(item, _schemaName);
                    }
                    else
                    {
                        var itemInsertData = item;
                        item.Id = Guid.NewGuid();
                        item.CreatedBy = username;
                        item.CreatedDate = DateTime.Now;
                        item.OrderRefNumber = model.OrderRefNumber;
                        _orderItemsRepository.Add(itemInsertData, _schemaName);
                    }
                }

                if (includeConfirm)
                {
                    var confirmResult = await CommonConfirm(model, username, token);
                    if (!confirmResult.IsSuccess)
                    {
                        return confirmResult;
                    }
                }

                _orderInformationsRepository.Save(_schemaName);
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

        public async Task<BaseResultModel> DeleteSOReturn(SaleOrderDetailQueryModel query, string username)
        {
            try
            {
                var baseModel = await CommonGetDetail(query);
                if (baseModel == null || baseModel.Count == 0)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "SO not found"
                    };
                }
                var headerDeleteData = _mapper.Map<SO_OrderInformations>(baseModel.Select(x => x.OrderInformation).FirstOrDefault());
                if (headerDeleteData.Status != SO_SaleOrderStatusConst.DRAFT)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "Only draft SO is allowed"
                    };
                }

                headerDeleteData.IsDeleted = true;
                headerDeleteData.UpdatedBy = username;
                headerDeleteData.UpdatedDate = DateTime.Now;
                _orderInformationsRepository.UpdateUnSaved(headerDeleteData, _schemaName);

                foreach (var item in baseModel.Where(x => x.OrderItem != null).Select(x => x.OrderItem).ToList())
                {
                    item.IsDeleted = true;
                    item.UpdatedBy = username;
                    item.UpdatedDate = DateTime.Now;
                    _orderItemsRepository.UpdateUnSaved(item, _schemaName);
                }

                _orderInformationsRepository.Save(_schemaName);
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

        public async Task<BaseResultModel> Confirm(SaleOrderModel model, string token, string username)
        {
            try
            {
                var baseModel = await CommonGetDetail(new SaleOrderDetailQueryModel { DistributorCode = model.DistributorCode, OrderRefNumber = model.OrderRefNumber });
                if (baseModel == null || baseModel.Count == 0)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "SaleOrder not found"
                    };
                }

                if (baseModel.First().OrderInformation.Status != SO_SaleOrderStatusConst.DRAFT)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "Only Confirm Darft Order"
                    };
                }

                foreach (var item in model.OrderItems)
                {
                    if (item.Id != Guid.Empty)
                    {
                        item.UpdatedBy = username;
                        item.UpdatedDate = DateTime.Now;
                        _orderItemsRepository.UpdateUnSaved(item, _schemaName);
                    }
                    else
                    {
                        var itemInsertData = item;
                        item.Id = Guid.NewGuid();
                        item.CreatedBy = username;
                        item.CreatedDate = DateTime.Now;
                        item.OrderRefNumber = model.OrderRefNumber;
                        _orderItemsRepository.Add(itemInsertData, _schemaName);
                    }
                }

                var calculateResult = CommonCalulateOrderHeader(ref model);
                model.Status = SO_SaleOrderStatusConst.CONFIRM;
                model.UpdatedBy = username;
                model.UpdatedDate = DateTime.Now;
                _orderInformationsRepository.UpdateUnSaved(model, _schemaName);

                var confirmResult = await CommonConfirm(model, username, token);
                if (!confirmResult.IsSuccess)
                {
                    return confirmResult;
                }
                _orderInformationsRepository.Save(_schemaName);
                return new BaseResultModel
                {
                    Code = 200,
                    IsSuccess = true,
                    Message = "OK",
                    Data = model,
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

        public async Task<BaseResultModel> GetDetailSOReturn(SaleOrderDetailQueryModel query)
        {

            try
            {
                query.DistributorCode = _distributorCode;

                var baseModel = await CommonGetDetail(query);
                SaleOrderModel detailModel = _mapper.Map<SaleOrderModel>(baseModel.Where(x => x.OrderInformation != null).Select(x => x.OrderInformation).FirstOrDefault());
                detailModel.OrderItems = new List<SO_OrderItems>();

                foreach (var item in baseModel.Where(x => x.OrderItem != null).Select(x => x.OrderItem).ToList())
                {
                    detailModel.OrderItems.Add(item);
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = detailModel
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

        public async Task<ResultModelWithObject<ListSOModel>> SearchSOReturn(SaleOrderSearchParamsModel parameters)
        {
            try
            {
                parameters.DistributorCode = _distributorCode;

                IQueryable<SO_OrderInformations> res = await CommonGetAllQueryable(parameters);
                if (res == null)
                {
                    return new ResultModelWithObject<ListSOModel>
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "Error while search SO",
                    };
                }

                if (parameters.FromDate.HasValue)
                {
                    res = res.Where(x => x.OrderDate.Date >= parameters.FromDate.Value.Date);
                }
                if (parameters.ToDate.HasValue)
                {
                    res = res.Where(x => x.OrderDate.Date <= parameters.ToDate.Value.Date);
                }

                var listSO = new List<SaleOrderModel>();

                if (parameters.IsDropdown)
                {
                    listSO = _mapper.Map<List<SaleOrderModel>>(res.ToList());

                    foreach (var item in listSO)
                    {
                        //item.OrderItems = res.Where(x => x.OrderItem != null).Select(x => x.OrderItem).Where(x => x.OrderRefNumber == item.OrderRefNumber).ToList();
                        item.OrderItems = _orderItemsRepository.GetAllQueryable(null, null, null, _schemaName).Where(x => x.OrderRefNumber == item.OrderRefNumber).ToList();
                    }

                    var page1 = PagedList<SaleOrderModel>.ToPagedList(listSO, 0, listSO.Count);

                    var reponse = new ListSOModel { Items = listSO };
                    return new ResultModelWithObject<ListSOModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = reponse
                    };
                }
                var poTempPagged = PagedList<SO_OrderInformations>.ToPagedListQueryAble(res, (parameters.PageNumber - 1) * parameters.PageSize, parameters.PageSize);
                listSO = _mapper.Map<List<SaleOrderModel>>(poTempPagged.Where(x => x != null).ToList());
                foreach (var item in listSO)
                {
                    item.OrderItems = _orderItemsRepository.GetAllQueryable(null, null, null, _schemaName).Where(x => x.OrderRefNumber == item.OrderRefNumber).ToList();
                }

                var repsonse = new ListSOModel { Items = listSO, MetaData = poTempPagged.MetaData };

                //return metadata
                return new ResultModelWithObject<ListSOModel>
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
                return new ResultModelWithObject<ListSOModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
    }
}
