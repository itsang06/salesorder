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
using SysAdmin.Models.StaticValue;
using Newtonsoft.Json;
using RestSharp.Authenticators;
using DynamicSchema.Helper.Services.Interface;
using DynamicSchema.Helper.Services;
using ODSaleOrder.API.Services.OrderStatusHistoryService;
using ODSaleOrder.API.Models.OS;
using ODSaleOrder.API.Services.OneShop.Interface;
using Microsoft.AspNetCore.Http;

namespace ODSaleOrder.API.Services.SaleOrder
{
    public class SumpickingService : ISumpickingService
    {
        private readonly ILogger<SumpickingService> _logger;
        private readonly IMapper _mapper;
        private readonly IDynamicBaseRepository<SO_OrderInformations> _orderInformationRepository;
        private readonly IDynamicBaseRepository<SO_OrderItems> _orderItemsRepository;
        private readonly IDynamicBaseRepository<SO_SumPickingListHeader> _sumPickingListHeaderRepository;
        private readonly IDynamicBaseRepository<SO_SumPickingListDetail> _sumPickingListDetailRepository;

        private readonly ISalesOrderService _saleOrderService;
        private readonly IPromotionsService _promService;
        public readonly IClientService _clientService;
        public IRestClient _client;
        private readonly IOrderStatusHistoryService _orderStatusHisService;
        private readonly IOSNotificationService _osNotifiService;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;
        private string _distributorCode = null;

        public SumpickingService(ILogger<SumpickingService> logger,
            IHttpContextAccessor httpContextAccessor,
            IMapper mapper,
            RDOSContext dataContext,
            ISalesOrderService saleOrderService,
            IPromotionsService promService,
            IClientService clientService,
            IOrderStatusHistoryService orderStatusHisService,
            IOSNotificationService osNotifiService)
        {
            _logger = logger;
            _mapper = mapper;
            _sumPickingListHeaderRepository = new DynamicBaseRepository<SO_SumPickingListHeader>(dataContext);
            _sumPickingListDetailRepository = new DynamicBaseRepository<SO_SumPickingListDetail>(dataContext);
            _orderInformationRepository = new DynamicBaseRepository<SO_OrderInformations>(dataContext);
            _orderItemsRepository = new DynamicBaseRepository<SO_OrderItems>(dataContext);
            _promService = promService;
            _clientService = clientService;
            _saleOrderService = saleOrderService;
            _orderStatusHisService = orderStatusHisService;
            _osNotifiService = osNotifiService;
            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
            _distributorCode = _httpContextAccessor.HttpContext?.Items["DistributorCode"] as string ?? _distributorCode;
        }

        public async Task<ResultModelWithObject<SumpickingDetailModel>> CommonGetDetailSumpicking(SumpickingDetailQueryModel query, string token, bool includeItems = true)
        {
            try
            {
                SumpickingDetailModel sumpickingDetail = new();
                //Get header
                SO_SumPickingListHeader sumpickingHeaderIndb = await _sumPickingListHeaderRepository.GetAllQueryable(null, null, null, _schemaName).Where(x =>
                    !x.IsDeleted &&
                    !string.IsNullOrWhiteSpace(x.DistributorCode) && x.DistributorCode == query.DistributorCode &&
                    !string.IsNullOrWhiteSpace(x.SumPickingRefNumber) && x.SumPickingRefNumber == query.SumPickingRefNumber).AsNoTracking().FirstOrDefaultAsync();
                if (sumpickingHeaderIndb == null)
                {
                    return new ResultModelWithObject<SumpickingDetailModel>
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = $"Sumpicking header {query.SumPickingRefNumber} not found",
                    };
                }
                sumpickingDetail.sumPickingListHeader = sumpickingHeaderIndb;

                //Get List SaleOrder
                List<SO_SumPickingListDetail> listOrder = await _sumPickingListDetailRepository.GetAllQueryable(null, null, null, _schemaName).Where(x =>
                    !x.IsDeleted &&
                    !string.IsNullOrWhiteSpace(x.SumPickingRefNumber) && x.SumPickingRefNumber == sumpickingHeaderIndb.SumPickingRefNumber)
                    .AsNoTracking().ToListAsync();
                
                if (includeItems && (listOrder != null || listOrder.Count > 0))
                {
                    sumpickingDetail.SumPickingListDetails = listOrder;

                    //Get List Items
                    List<string> listOrderRefNumber = listOrder.Select(x => x.OrderRefNumber).ToList();
                    if (listOrderRefNumber != null || listOrderRefNumber.Count > 0)
                    {
                        //SumItem
                        SumpickingItemDetailModel sumpickingItemDetailModel = await CommonGetSumpickingItems(listOrderRefNumber, token);

                        sumpickingDetail.SumpickingItems = sumpickingItemDetailModel.SumpickingItem;

                        sumpickingDetail.PrintItems = sumpickingItemDetailModel.PrintItems;
                    }
                }
                return new ResultModelWithObject<SumpickingDetailModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = sumpickingDetail
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<SumpickingDetailModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<SumpickingItemDetailModel> CommonGetSumpickingItems(List<string> listOrderRefNumber, string token)
        {
            try
            {
                var saleOrderItems = await _saleOrderService.CommonGetOrderDetailsRefNumber(listOrderRefNumber);
                List<SumpickingItemModel> sumpickingItems = new();
                var selectedItems = saleOrderItems.Where(x =>
                    !(x.IsKit && string.IsNullOrWhiteSpace(x.ItemCode)) &&
                    !(!string.IsNullOrWhiteSpace(x.PromotionCode) && string.IsNullOrWhiteSpace(x.ItemCode))).ToList();
                
                foreach (var item in selectedItems)
                {
                    if (!sumpickingItems.Any(x => x.InventoryID == item.InventoryID))
                    {
                        SumpickingItemModel sumpickingItem = new();
                        sumpickingItem.InventoryID = item.InventoryID;
                        sumpickingItem.Description = item.ItemDescription;
                        sumpickingItem.Orig_Ord_Qty += item.OriginalOrderBaseQuantities;
                        sumpickingItem.Ord_Qty += item.OrderBaseQuantities;
                        sumpickingItem.Shipped_Qty += item.ShippedBaseQuantities;
                        sumpickingItem.ShippingQuantities += item.ShippingBaseQuantities; // cần lấy base Shipping Qty
                        sumpickingItem.FailedQuantities += item.FailedBaseQuantities; // cần lấy base failedQuantities
                        sumpickingItem.RemainQuantities += item.RemainQuantities; // cần lấy remain BaseQty
                        sumpickingItem.OrderBaseQuantities += item.OrderBaseQuantities;
                        sumpickingItem.PurchaseUnit = item.PurchaseUnit;
                        sumpickingItem.SalesUnit = item.SalesUnitCode;
                        sumpickingItem.BaseUnit = item.BaseUnitCode;
                        sumpickingItems.Add(sumpickingItem);
                    }
                    else
                    {
                        SumpickingItemModel existedItem = sumpickingItems.First(x => x.InventoryID == item.InventoryID);
                        existedItem.InventoryID = item.InventoryID;
                        existedItem.Description = item.ItemDescription;
                        existedItem.Orig_Ord_Qty += item.OriginalOrderBaseQuantities;
                        existedItem.Ord_Qty += item.OrderBaseQuantities;
                        existedItem.Shipped_Qty += item.ShippedBaseQuantities;
                        existedItem.ShippingQuantities += item.ShippingBaseQuantities; // cần lấy base Shipping Qty
                        existedItem.FailedQuantities += item.FailedBaseQuantities; // cần lấy base failedQuantities
                        existedItem.RemainQuantities += item.RemainQuantities; // cần lấy remain BaseQty
                        existedItem.OrderBaseQuantities += item.OrderBaseQuantities;
                    }
                }

                var newSumpickingItems = new List<SumpickingItemModel>();
                foreach (var item in sumpickingItems)
                {
                    var detailItem = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryItemByCode/{item.InventoryID}", Method.GET, token, null);

                    var Sales_FailedQuantities = await _promService.CommonGetQtyFromUnitIdToUnitId(detailItem.UomConversion, detailItem.InventoryItem.BaseUnit, detailItem.InventoryItem.SalesUnit, item.FailedQuantities);
                    var Sales_Ord_Qty = await _promService.CommonGetQtyFromUnitIdToUnitId(detailItem.UomConversion, detailItem.InventoryItem.BaseUnit, detailItem.InventoryItem.SalesUnit, item.Ord_Qty);
                    var Sales_OrderBaseQuantities = await _promService.CommonGetQtyFromUnitIdToUnitId(detailItem.UomConversion, detailItem.InventoryItem.BaseUnit, detailItem.InventoryItem.SalesUnit, item.OrderBaseQuantities);
                    var Sales_Orig_Ord_Qty = await _promService.CommonGetQtyFromUnitIdToUnitId(detailItem.UomConversion, detailItem.InventoryItem.BaseUnit, detailItem.InventoryItem.SalesUnit, item.Orig_Ord_Qty);
                    var Sales_RemainQuantities = await _promService.CommonGetQtyFromUnitIdToUnitId(detailItem.UomConversion, detailItem.InventoryItem.BaseUnit, detailItem.InventoryItem.SalesUnit, item.RemainQuantities);
                    var Sales_Shipped_Qty = await _promService.CommonGetQtyFromUnitIdToUnitId(detailItem.UomConversion, detailItem.InventoryItem.BaseUnit, detailItem.InventoryItem.SalesUnit, item.Shipped_Qty);
                    var Sales_ShippingQuantities = await _promService.CommonGetQtyFromUnitIdToUnitId(detailItem.UomConversion, detailItem.InventoryItem.BaseUnit, detailItem.InventoryItem.SalesUnit, item.ShippingQuantities);

                    var SalesBase_FailedQuantities = await _promService.CommonGetQtyFromUnitIdToUnitId(detailItem.UomConversion, detailItem.InventoryItem.SalesUnit, detailItem.InventoryItem.BaseUnit, Sales_FailedQuantities);
                    var SalesBase_Ord_Qty = await _promService.CommonGetQtyFromUnitIdToUnitId(detailItem.UomConversion, detailItem.InventoryItem.SalesUnit, detailItem.InventoryItem.BaseUnit, Sales_Ord_Qty);
                    var SalesBase_OrderBaseQuantities = await _promService.CommonGetQtyFromUnitIdToUnitId(detailItem.UomConversion, detailItem.InventoryItem.SalesUnit, detailItem.InventoryItem.BaseUnit, Sales_OrderBaseQuantities);
                    var SalesBase_Orig_Ord_Qty = await _promService.CommonGetQtyFromUnitIdToUnitId(detailItem.UomConversion, detailItem.InventoryItem.SalesUnit, detailItem.InventoryItem.BaseUnit, Sales_Orig_Ord_Qty);
                    var SalesBase_RemainQuantities = await _promService.CommonGetQtyFromUnitIdToUnitId(detailItem.UomConversion, detailItem.InventoryItem.SalesUnit, detailItem.InventoryItem.BaseUnit, Sales_RemainQuantities);
                    var SalesBase_Shipped_Qty = await _promService.CommonGetQtyFromUnitIdToUnitId(detailItem.UomConversion, detailItem.InventoryItem.SalesUnit, detailItem.InventoryItem.BaseUnit, Sales_Shipped_Qty);
                    var SalesBase_ShippingQuantities = await _promService.CommonGetQtyFromUnitIdToUnitId(detailItem.UomConversion, detailItem.InventoryItem.SalesUnit, detailItem.InventoryItem.BaseUnit, Sales_ShippingQuantities);

                    
                    SalesBase_FailedQuantities = item.FailedQuantities - SalesBase_FailedQuantities;
                    SalesBase_Ord_Qty = item.Ord_Qty - SalesBase_Ord_Qty;
                    SalesBase_OrderBaseQuantities = item.OrderBaseQuantities - SalesBase_OrderBaseQuantities;
                    SalesBase_Orig_Ord_Qty = item.Orig_Ord_Qty - SalesBase_Orig_Ord_Qty;
                    SalesBase_RemainQuantities = item.RemainQuantities - SalesBase_RemainQuantities;
                    SalesBase_Shipped_Qty = item.Shipped_Qty - SalesBase_Shipped_Qty;
                    SalesBase_ShippingQuantities = item.ShippingQuantities - SalesBase_ShippingQuantities;

                    newSumpickingItems.Add(new SumpickingItemModel
                    {
                        InventoryID = item.InventoryID,
                        Description = item.Description,
                        Orig_Ord_Qty = SalesBase_Orig_Ord_Qty,
                        Ord_Qty = SalesBase_Ord_Qty,
                        Shipped_Qty = SalesBase_Shipped_Qty,
                        OrderBaseQuantities = SalesBase_OrderBaseQuantities,
                        ShippingQuantities = SalesBase_ShippingQuantities,
                        FailedQuantities = SalesBase_FailedQuantities,
                        RemainQuantities = SalesBase_RemainQuantities,
                        Sales_Orig_Ord_Qty = Sales_Orig_Ord_Qty,
                        Sales_Ord_Qty = Sales_Ord_Qty,
                        Sales_Shipped_Qty = Sales_Shipped_Qty,
                        Sales_OrderBaseQuantities = Sales_OrderBaseQuantities,
                        Sales_ShippingQuantities = Sales_ShippingQuantities,
                        Sales_FailedQuantities = Sales_FailedQuantities,
                        Sales_RemainQuantities = Sales_RemainQuantities,
                        SalesUnit = item.SalesUnit,
                        BaseUnit = item.BaseUnit,
                        PurchaseUnit = item.PurchaseUnit,
                    });
                }

                selectedItems = selectedItems.GroupBy(x => new
                {
                    x.ItemCode,
                }).Select(x => new SO_OrderItems
                {
                    Id = x.Select(x => x.Id).FirstOrDefault(),
                    OrderRefNumber = x.Select(x => x.OrderRefNumber).FirstOrDefault(),
                    InventoryID = x.Select(x => x.InventoryID).FirstOrDefault(),
                    KitId = x.Select(x => x.KitId).FirstOrDefault(),
                    IsKit = x.Select(x => x.IsKit).FirstOrDefault(),
                    PromotionCode = x.Select(x => x.PromotionCode).FirstOrDefault(),
                    LocationID = x.Select(x => x.LocationID).FirstOrDefault(),
                    ItemId = x.Select(x => x.ItemId).FirstOrDefault(),
                    ItemCode = x.Select(x => x.ItemCode).FirstOrDefault(),
                    ItemDescription = x.Select(x => x.ItemDescription).FirstOrDefault(),
                    UOM = x.Select(x => x.UOM).FirstOrDefault(),
                    UnitRate = x.Select(x => x.UnitRate).FirstOrDefault(),
                    OriginalOrderBaseQuantities = x.Sum(x => x.OriginalOrderBaseQuantities),
                    OrderBaseQuantities = x.Sum(x => x.OrderBaseQuantities),
                    ShippedBaseQuantities = x.Sum(x => x.ShippedBaseQuantities),
                    PurchaseUnit = x.Where(x => x.PurchaseUnit.HasValue && x.PurchaseUnit.Value != Guid.Empty).Select(x => x.PurchaseUnit).FirstOrDefault(),
                }).ToList();
                return new SumpickingItemDetailModel
                {
                    PrintItems = selectedItems,
                    SumpickingItem = newSumpickingItems
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new SumpickingItemDetailModel();
            }
        }

        public async Task<BaseResultModel> GetSumpickingItems(List<string> listOrderRefNumber, string token)
        {
            try
            {
                var result = await CommonGetSumpickingItems(listOrderRefNumber, token);
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = result
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


        public async Task<ResultModelWithObject<SumpickingDetailModel>> GetDetailSumpicking(SumpickingDetailQueryModel query, string token, bool includeItems = true)
        {
            try
            {
                //if (IsODSiteConstant)
                //{
                    query.DistributorCode = _distributorCode;
                //}

                var result = await CommonGetDetailSumpicking(query, token, includeItems);
                _orderInformationRepository.Dispose(_schemaName);
                return result;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<SumpickingDetailModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
        public async Task<BaseResultModel> SearchSumpicking(SumpickingSearchModel parameters)
        {
            try
            {
                //if (IsODSiteConstant) 
                //{
                    parameters.DistributorCode = _distributorCode;
                //}

                List<SO_SumPickingListHeader> res = new List<SO_SumPickingListHeader>();
                if (parameters.SaleOrders != null && parameters.SaleOrders.Count > 0)
                {
                    List<string> headerSumpickingRef = _sumPickingListDetailRepository.GetAllQueryable(null, null, null, _schemaName)
                        .Where(x => x.OrderRefNumber != null && parameters.SaleOrders.Contains(x.OrderRefNumber))
                        .Select(x => x.SumPickingRefNumber)
                        .ToList();

                    res = _sumPickingListHeaderRepository.GetAllQueryable(null, null, null, _schemaName)
                        .Where(x =>
                            (!string.IsNullOrWhiteSpace(parameters.DistributorCode) ? x.DistributorCode != null && x.DistributorCode == parameters.DistributorCode : true) &&
                            !x.IsDeleted &&
                            headerSumpickingRef.Contains(x.SumPickingRefNumber))
                        .AsNoTracking()
                        .OrderByDescending(x => x.SumPickingRefNumber)
                        .ToList();
                }
                else
                {

                    res = _sumPickingListHeaderRepository.GetAllQueryable(null, null, null, _schemaName)
                        .Where(x => 
                            (!string.IsNullOrWhiteSpace(parameters.DistributorCode) ? x.DistributorCode != null && x.DistributorCode == parameters.DistributorCode : true) 
                            && !x.IsDeleted)
                        .AsNoTracking()
                        .OrderByDescending(x => x.SumPickingRefNumber)
                        .ToList();
                }

                if (parameters.FromDate.HasValue)
                {
                    res = res.Where(x => x.TransactionDate.HasValue && x.TransactionDate.Value.Date >= parameters.FromDate.Value.Date).ToList();
                }
                if (parameters.ToDate.HasValue)
                {
                    res = res.Where(x => x.TransactionDate.HasValue && x.TransactionDate.Value.Date <= parameters.ToDate.Value.Date).ToList();
                }

                //if has filter expression
                if (parameters.Filters != null && parameters.Filters.Count > 0)
                {
                    foreach (var filter in parameters.Filters)
                    {
                        var getter = typeof(SO_SumPickingListHeader).GetProperty(filter.Property);
                        res = res.Where(x => filter.Values.Any(a => a == "" || a == null) ?
                                string.IsNullOrEmpty(getter.GetValue(x, null).EmptyIfNull().ToString()) || filter.Values.Contains(getter.GetValue(x, null).ToString().ToLower().Trim()) :
                                !string.IsNullOrEmpty(getter.GetValue(x, null).EmptyIfNull().ToString()) && filter.Values.Contains(getter.GetValue(x, null).ToString().ToLower().Trim())).ToList();
                    }
                }

                if (!string.IsNullOrWhiteSpace(parameters.SearchValue))
                {
                    res = res.Where(x =>
                        !string.IsNullOrWhiteSpace(x.SumPickingRefNumber) && x.SumPickingRefNumber.ToLower().Trim() == parameters.SearchValue.ToLower().Trim() ||
                        !string.IsNullOrWhiteSpace(x.DriverCode) && x.DriverCode.ToLower().Trim() == parameters.SearchValue.ToLower().Trim()
                    ).ToList();
                }

                var resDetails = _sumPickingListDetailRepository
                    .GetAllQueryable(x => !string.IsNullOrWhiteSpace(x.SumPickingRefNumber) && res.Select(x => x.SumPickingRefNumber)
                    .ToList()
                    .Contains(x.SumPickingRefNumber), null, null, _schemaName)
                    .ToList();
                
                List<SumpickingModel> response = res.Select(x => new SumpickingModel
                {
                    Id = x.Id,
                    SumPickingRefNumber = x.SumPickingRefNumber,
                    DistributorCode = x.DistributorCode,
                    TransactionDate = x.TransactionDate,
                    Status = x.Status,
                    Vehicle = x.Vehicle,
                    DriverCode = x.DriverCode,
                    WareHouseID = x.WareHouseID,
                    NumberPlates = x.NumberPlates,
                    VehicleLoad = x.VehicleLoad,
                    TotalWeight = x.TotalWeight,
                    IsPrinted = x.IsPrinted,
                    PrintedCount = x.PrintedCount,
                    LastedPrintDate = x.LastedPrintDate,
                    TotalOrderQuantities = x.TotalOrderQuantities,
                    TotalOriginOrderQuantities = x.TotalOriginOrderQuantities,
                    TotalShippedQuantities = x.TotalShippedQuantities,
                    TotalFailedQuantities = x.TotalFailedQuantities,
                    TotalShippingQuantities = x.TotalShippingQuantities,
                    TotalRemainQuantities = x.TotalRemainQuantities,
                    SumPickingListDetails = resDetails.Where(y => y.SumPickingRefNumber == x.SumPickingRefNumber).ToList()
                }).ToList();
                if (parameters.IsDropdown)
                {
                    var reponse = new ListSumpickingModel { Items = response };
                    return new BaseResultModel
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = reponse
                    };
                }

                var sumpickingTempPagged = PagedList<SumpickingModel>.ToPagedList(response, (parameters.PageNumber - 1) * parameters.PageSize, parameters.PageSize);
                var repsonse = new ListSumpickingModel { Items = sumpickingTempPagged, MetaData = sumpickingTempPagged.MetaData };

                _sumPickingListDetailRepository.Dispose(_schemaName);
                //return metadata
                return new BaseResultModel
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
                return new BaseResultModel
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<ResultModelWithObject<List<SumpickingItemModel>>> CalculateSumpickingOrders(List<SaleOrderModel> listSaleOrder, List<string> orderRefNumbers)
        {
            try
            {
                if (listSaleOrder.Count != orderRefNumbers.Count)
                {
                    List<string> missingOrders = new();
                    foreach (var orderRefNumber in orderRefNumbers)
                    {
                        if (!listSaleOrder.Any(x => x.OrderRefNumber == orderRefNumber))
                        {
                            missingOrders.Add(orderRefNumber);
                        }
                    }
                    var message = "";
                    foreach (var item in missingOrders)
                    {
                        message += $" {item},";
                    }
                    return new ResultModelWithObject<List<SumpickingItemModel>>
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = $"SaleOrder {message} not found",
                    };
                }

                #region Calculate
                List<string> groupedInventoryItemList = new();
                List<SO_OrderItems> orderItemList = new();
                foreach (var listOrderItems in listSaleOrder.Where(x => x.OrderItems != null && x.OrderItems.Count > 0).Select(x => x.OrderItems).ToList())
                {
                    groupedInventoryItemList.AddRange(listOrderItems.Select(x => x.InventoryID).ToList());
                    orderItemList.AddRange(listOrderItems);
                }
                groupedInventoryItemList = groupedInventoryItemList.Distinct().ToList();
                List<SumpickingItemModel> sumpickingItems = new();
                foreach (var groupedInventoryItem in groupedInventoryItemList)
                {
                    SumpickingItemModel sumpickingItem = new();
                    sumpickingItem.InventoryID = groupedInventoryItem;
                    foreach (var item in orderItemList.Where(x => x.InventoryID == groupedInventoryItem).ToList())
                    {
                        sumpickingItem.Description = item.ItemDescription;
                        sumpickingItem.Orig_Ord_Qty += item.OriginalOrderBaseQuantities;
                        sumpickingItem.Ord_Qty += item.OrderBaseQuantities;
                        sumpickingItem.Shipped_Qty += item.ShippedBaseQuantities;
                        sumpickingItem.ShippingQuantities += item.ShippingBaseQuantities;
                        sumpickingItem.FailedQuantities += item.FailedBaseQuantities;
                        sumpickingItem.RemainQuantities += item.RemainQuantities;
                    }
                    sumpickingItems.Add(sumpickingItem);
                }
                #endregion
                return new ResultModelWithObject<List<SumpickingItemModel>>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
                    Data = sumpickingItems,
                };
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<List<SumpickingItemModel>>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }

        public async Task<BaseResultModel> SaveWithConfirm(SumpickingModel model, string token, string username)
        {
            try
            {
                if (model.Id != Guid.Empty)
                {
                    return await Update(model, token, username, true);
                }
                else
                {
                    return await Insert(model, token, username, true);

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

        public async Task<BaseResultModel> Insert(SumpickingModel model, string token, string username, bool includeConfirm = false)
        {
            try
            {
                //if (IsODSiteConstant)
                //{
                    model.DistributorCode = _distributorCode;
                    model.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                    model.OwnerCode = _distributorCode;
                //}

                if (string.IsNullOrWhiteSpace(model.DistributorCode))
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "missing DistributorCode",
                    };
                }

                //Generate RefNumber
                var prefix = StringsHelper.GetPrefixYYM();
                var refNumber = await _sumPickingListHeaderRepository.GetAllQueryable(null, null, null, _schemaName)
                    .Where(x => x.SumPickingRefNumber.Contains(prefix)).AsNoTracking().Select(x => x.SumPickingRefNumber).OrderByDescending(x => x).FirstOrDefaultAsync();
                var generatedNumber = StringsHelper.GennerateCodeWithYearMonthFormat(prefix, refNumber != null ? refNumber : null);

                List<string> listOrderRefNumber = model.SumPickingListDetails.Select(x => x.OrderRefNumber).ToList();

                var listSaleOrder = (await _saleOrderService.SearchSOv2(new SaleOrderSearchParamsModel
                {
                    IsDropdown = true,
                    DistributorCode = model.DistributorCode,
                    OrderRefNumbers = listOrderRefNumber,
                    IncludeItem = true,
                }, null, false, true)).Data?.Items ?? new List<SaleOrderModel>();

                var calResult = await CalculateSumpickingOrders(listSaleOrder, model.SumPickingListDetails.Select(x => x.OrderRefNumber).ToList());
                if (!calResult.IsSuccess)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = calResult.Message,
                    };
                }

                var sumpickingItems = calResult.Data;
                foreach (var item in sumpickingItems)
                {
                    model.TotalOrderQuantities += item.Ord_Qty;
                    model.TotalOriginOrderQuantities += item.Orig_Ord_Qty;
                    model.TotalShippedQuantities += item.Shipped_Qty;
                    model.TotalFailedQuantities += item.FailedQuantities;
                    model.TotalShippingQuantities += item.ShippingQuantities;
                    model.TotalRemainQuantities += item.RemainQuantities;
                }

                model.CreatedBy = username;
                model.CreatedDate = DateTime.Now;
                model.SumPickingRefNumber = generatedNumber;
                model.Status = includeConfirm ? SO_SaleOrderStatusConst.CONFIRM : SO_SaleOrderStatusConst.DRAFT;
                _sumPickingListHeaderRepository.Add(model, _schemaName);
                foreach (var item in model.SumPickingListDetails)
                {
                    item.CreatedBy = username;
                    item.CreatedDate = DateTime.Now;
                    item.SumPickingRefNumber = model.SumPickingRefNumber;
                    //if (IsODSiteConstant)
                    //{
                        item.OwnerCode = _distributorCode;
                        item.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                    //}
                    _sumPickingListDetailRepository.Add(item, _schemaName);
                }

                if (includeConfirm)
                {
                    var transactionResult = await CommonConfirm(model, token, username, listSaleOrder);
                    if (!transactionResult.IsSuccess)
                    {
                        return transactionResult;
                    }
                }

                _sumPickingListDetailRepository.Save(_schemaName);
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

        public async Task<BaseResultModel> Update(SumpickingModel model, string token, string username, bool includeConfirm = false)
        {
            try
            {
                List<string> listOrderRefNumber = model.SumPickingListDetails.Select(x => x.OrderRefNumber).ToList();

                //if (IsODSiteConstant)
                //{
                model.DistributorCode = _distributorCode;
                model.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                model.OwnerCode = _distributorCode;
                //}

                var listSaleOrder = (await _saleOrderService.SearchSOv2(new SaleOrderSearchParamsModel
                {
                    IsDropdown = true,
                    DistributorCode = model.DistributorCode,
                    OrderRefNumbers = listOrderRefNumber,
                    IncludeItem = true,
                }, null, false, true)).Data?.Items ?? new List<SaleOrderModel>();

                //var calResult = await CalculateSumpickingOrders(model.SumPickingListDetails.Select(x => x.OrderRefNumber).ToList(), model.DistributorCode);
                var calResult = await CalculateSumpickingOrders(listSaleOrder, listOrderRefNumber);

                if (!calResult.IsSuccess)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = calResult.Message,
                    };
                }
                var sumpickingItems = calResult.Data;


                //re-calculate
                model.TotalOrderQuantities = 0;
                model.TotalOriginOrderQuantities = 0;
                model.TotalShippedQuantities = 0;
                model.TotalFailedQuantities = 0;
                model.TotalShippingQuantities = 0;
                model.TotalRemainQuantities = 0;
                foreach (var item in sumpickingItems)
                {
                    model.TotalOrderQuantities += item.Ord_Qty;
                    model.TotalOriginOrderQuantities += item.Orig_Ord_Qty;
                    model.TotalShippedQuantities += item.Shipped_Qty;
                    model.TotalFailedQuantities += item.FailedQuantities;
                    model.TotalShippingQuantities += item.ShippingQuantities;
                    model.TotalRemainQuantities += item.RemainQuantities;
                }

                model.UpdatedBy = username;
                model.UpdatedDate = DateTime.Now;
                model.Status = includeConfirm ? SO_SaleOrderStatusConst.CONFIRM : model.Status;
                _sumPickingListHeaderRepository.UpdateUnSaved(model, _schemaName);
                foreach (var item in model.SumPickingListDetails)
                {
                    //if (IsODSiteConstant)
                    //{
                    item.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                    item.OwnerCode = _distributorCode;
                    //}

                    if (item.Id == Guid.Empty)
                    {
                        item.CreatedBy = username;
                        item.CreatedDate = DateTime.Now;
                        _sumPickingListDetailRepository.Add(item, _schemaName);
                    }
                    else
                    {
                        item.UpdatedBy = username;
                        item.UpdatedDate = DateTime.Now;
                        _sumPickingListDetailRepository.UpdateUnSaved(item, _schemaName);
                    }
                }

                if (includeConfirm)
                {
                    var transactionResult = await CommonConfirm(model, token, username, listSaleOrder);
                    if (!transactionResult.IsSuccess)
                    {
                        return transactionResult;
                    }
                }
                _sumPickingListDetailRepository.Save(_schemaName);
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

        public async Task<BaseResultModel> CommonConfirm(SumpickingModel model, string token, string username, List<SaleOrderModel> listSaleOrder = null)
        {
            try
            {
                List<INV_TransactionModel> transactionData = new();
                if (listSaleOrder == null)
                {
                    listSaleOrder = (await _saleOrderService.SearchSOv2(new SaleOrderSearchParamsModel
                    {
                        IsDropdown = true,
                        DistributorCode = model.DistributorCode,
                        OrderRefNumbers = model.SumPickingListDetails.Select(x => x.OrderRefNumber).ToList(),
                        IncludeItem = true
                    }, null, false, true)).Data?.Items ?? new List<SaleOrderModel>();
                }
                
                foreach (var saleOrder in listSaleOrder)
                {
                    // Validate duplicate confirm
                    var resultValidate = await ValidateConfirm(saleOrder.OrderRefNumber);
                    if (!resultValidate.IsSuccess) return resultValidate;

                    saleOrder.Status = SO_SaleOrderStatusConst.SHIPPING;
                    saleOrder.UpdatedBy = username;
                    saleOrder.UpdatedDate = DateTime.Now;
                    saleOrder.ShipDate = DateTime.Now;
                    _orderInformationRepository.UpdateUnSaved(saleOrder, _schemaName);

                    var _osStatus = new ODMappingOrderStatus();
                    if (!string.IsNullOrWhiteSpace(saleOrder.External_OrdNBR)
                        && saleOrder.Source == SO_SOURCE_CONST.ONESHOP)
                    {
                        _osStatus = await _orderStatusHisService.HandleOSMappingStatus(saleOrder.Status);
                    }
                    else {
                        _osStatus = null;
                    }

                    saleOrder.OSStatus = _osStatus?.OneShopOrderStatus;

                    // Save history
                    OsorderStatusHistory hisStatusNew = new();
                    hisStatusNew.OrderRefNumber = saleOrder.OrderRefNumber;
                    hisStatusNew.ExternalOrdNbr = saleOrder.External_OrdNBR;
                    hisStatusNew.OrderDate = saleOrder.OrderDate;
                    hisStatusNew.DistributorCode = _distributorCode;
                    hisStatusNew.Sostatus = saleOrder.Status;
                    hisStatusNew.SOStatusName = _osStatus?.SaleOrderStatusName;
                    hisStatusNew.CreatedBy = username;
                    hisStatusNew.OutletCode = saleOrder.OSOutletCode;
                    hisStatusNew.OneShopStatus = _osStatus?.OneShopOrderStatus;
                    hisStatusNew.OneShopStatusName = _osStatus?.OneShopOrderStatusName;
                    BaseResultModel resultSaveStatusHis = await _orderStatusHisService.SaveStatusHistory(hisStatusNew, false);
                    if (!resultSaveStatusHis.IsSuccess) return resultSaveStatusHis;

                    foreach (var item in saleOrder.OrderItems)
                    {
                        if (!item.IsDeleted && !(item.IsKit && item.ItemCode == null) && !(item.PromotionCode != null && item.ItemCode == null))
                        {
                            item.ShippingQuantities = item.OrderQuantities;
                            item.ShippingBaseQuantities = item.OrderBaseQuantities;
                            transactionData.Add(new INV_TransactionModel
                            {
                                OrderCode = model.SumPickingRefNumber,
                                ItemId = item.ItemId,
                                ItemCode = item.ItemCode,
                                ItemDescription = item.ItemDescription,
                                Uom = item.UOM,
                                Quantity = item.OrderQuantities, // số lượng cần đặt
                                BaseQuantity = item.ShippingBaseQuantities, //base của quantity theo base uom
                                OrderBaseQuantity = item.OrderBaseQuantities,
                                TransactionDate = DateTime.Now,
                                TransactionType = INV_TransactionType.SO_PICKING,
                                WareHouseCode = model.WareHouseID,
                                LocationCode = item.LocationID,
                                DistributorCode = model.DistributorCode,
                                DSACode = null,
                                Description = null
                            });
                            _orderItemsRepository.UpdateUnSaved(item, _schemaName);
                        }
                    }
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

                foreach (var saleOrder in listSaleOrder)
                {
                    Serilog.Log.Information($"############ Chuẩn bị vào SendNotification : {saleOrder.OSStatus} - {saleOrder.Status}");
                    if (saleOrder.OSStatus != null)
                    {
                        // Send notification
                        OSNotificationModel reqNoti = new();
                        reqNoti.External_OrdNBR = saleOrder.External_OrdNBR;
                        reqNoti.OrderRefNumber = saleOrder.OrderRefNumber;
                        reqNoti.OSStatus = saleOrder.OSStatus;
                        reqNoti.SOStatus = saleOrder.SOStatus;
                        reqNoti.DistributorCode = saleOrder.DistributorCode;
                        reqNoti.DistributorName = saleOrder.DistributorName;
                        reqNoti.OutletCode = saleOrder.OSOutletCode;
                        reqNoti.Purpose = OSNotificationPurpose.GetPurpose(saleOrder.Status);

                        await _osNotifiService.SendNotification(reqNoti, token);
                    }
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "OK",
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

        public async Task<BaseResultModel> Confirm(SumpickingModel model, string token, string username)
        {
            try
            {
                //if (IsODSiteConstant)
                //{
                    model.DistributorCode = _distributorCode;
                //}

                var detail = await CommonGetDetailSumpicking(new SumpickingDetailQueryModel
                {
                    DistributorCode = model.DistributorCode,
                    SumPickingRefNumber = model.SumPickingRefNumber
                }, token, false);

                if (detail.Data == null)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = "Sumpicking notFound",
                        Data = model
                    };
                }

                var sumpickingDetail = detail.Data;
                model.Status = SO_SaleOrderStatusConst.CONFIRM;
                model.UpdatedDate = DateTime.Today;
                model.UpdatedBy = username;
                _sumPickingListHeaderRepository.UpdateUnSaved(model, _schemaName);
                foreach (var item in model.SumPickingListDetails)
                {
                    if (item.Id == Guid.Empty)
                    {
                        _sumPickingListDetailRepository.Add(item, _schemaName);
                    }
                    else
                    {
                        _sumPickingListDetailRepository.UpdateUnSaved(item, _schemaName);
                    }
                }

                var transactionResult = await CommonConfirm(model, token, username);
                if (!transactionResult.IsSuccess)
                {
                    return transactionResult;
                }

                _orderInformationRepository.Save(_schemaName);
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

        public async Task<BaseResultModel> ValidateConfirm(string soNumber)
        {
            try
            {
                var listDataInDb = await _sumPickingListDetailRepository
                    .GetAllQueryable(x => x.OrderRefNumber == soNumber && !x.IsDeleted, null, null, _schemaName)
                    .ToListAsync();

                if (listDataInDb != null && listDataInDb.Count > 0)
                {
                    foreach (var dataInDb in listDataInDb)
                    {
                        var dataHeaderInDb = await _sumPickingListHeaderRepository.GetAllQueryable(null, null, null, _schemaName)
                            .FirstOrDefaultAsync(x => x.SumPickingRefNumber == dataInDb.SumPickingRefNumber &&
                                                    x.Status == SO_SaleOrderStatusConst.CONFIRM && !x.IsDeleted);

                        if (dataHeaderInDb != null)
                        {
                            return new BaseResultModel
                            {
                                IsSuccess = false,
                                Code = 400,
                                Message = $"Sales order code: {soNumber} had been confirmed at sumpicking {dataHeaderInDb.SumPickingRefNumber}"
                            };
                        }
                    }
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
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
    }
}
