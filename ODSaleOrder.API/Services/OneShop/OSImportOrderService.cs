using AutoMapper;
using DynamicSchema.Helper.Services;
using DynamicSchema.Helper.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using ODSaleOrder.API.Models;
using ODSaleOrder.API.Models.OS;
using ODSaleOrder.API.Services.BaseLine;
using ODSaleOrder.API.Services.Inventory;
using ODSaleOrder.API.Services.OneShop.Interface;
using ODSaleOrder.API.Services.OrderStatusHistoryService;
using ODSaleOrder.API.Services.SaleOrder;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using RDOS.INVAPI.Infratructure;
using RestSharp;
using Sys.Common.Helper;
using Sys.Common.Models;
using SysAdmin.Models.StaticValue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using static SysAdmin.API.Constants.Constant;

namespace ODSaleOrder.API.Services.OneShop
{
    public class OSImportOrderService : IOSImportOrderService
    {
        // Private
        private readonly IDynamicBaseRepository<OsOrderInformation> _osOrderRepository;
        private readonly IDynamicBaseRepository<OsOrderItem> _osOrderItemRepository;
        private readonly IDynamicBaseRepository<SO_OrderInformations> _orderInformationsRepository;
        private readonly IDynamicBaseRepository<SO_SalesOrderSetting> _settingRepository;
        private readonly IDynamicBaseRepository<INV_AllocationDetail> _allocationDetailRepo;
        private readonly IDynamicBaseRepository<InvAllocationTracking> _alocationtrackinglogRepo;
        private readonly IDynamicBaseRepository<ODMappingOrderStatus> _odMappingOrderStatusRepo;

        // Public
        private readonly IDynamicBaseRepository<Kit> _kitRepository;
        private readonly IDynamicBaseRepository<Vat> _vatRepository;
        private readonly IDynamicBaseRepository<Principal> _principalRepository;

        // Service
        private readonly ILogger<OSImportOrderService> _logger;
        private readonly IInventoryService _inventoryService;
        private readonly IClientService _clientService;
        private readonly IMapper _mapper;
        private readonly ISalesOrderService _salesOrderService;
        private readonly IBaseLineService _baselineService;
        private readonly IOSNotificationService _osNotifiService;
        private readonly IOrderStatusHistoryService _orderStatusHisService;

        // Other
        private List<ExInventoryItemModel> _listInventoryItem = new List<ExInventoryItemModel>();
        private PrincipalWarehouseLocation _locationDefaultCurrent = null;
        private List<Vat> _listVats = new List<Vat>();
        private DistributorInfoModel _distributorInfo = null;
        private ExGetInfoCusAndShioptoByOutletCodeModel _customerInfoCurrent = null;
        private string _createdBy = null;
        private string _osStatusCurrent = null;

        private List<INV_InventoryTransaction> _listInvTransactionByOneShopId = new List<INV_InventoryTransaction>();
        private List<MdmModel> _mdmDistributor = null;
        private string _periodID = null;

        // Create so order item
        private Guid _KitId = Guid.Empty;
        private List<SO_OrderItems> _listSOOrderItem;

        private string _warehouseCode = null;
        private ExInventoryItemModel _detailSKUCurrent = null;

        // Validate import
        private bool _stockImport = true;
        private bool _stockImportAll = true;
        private bool _importBudgetStatus = true;
        private Guid _osOrderId = Guid.Empty;
        private string _oneShopId = null;

        private RDOSContext dataContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;
        private string _distributorCode = null;
        public OSImportOrderService(
            IHttpContextAccessor httpContextAccessor,
            RDOSContext _dataContext,
            // Service
            ILogger<OSImportOrderService> logger,
            IInventoryService inventoryService,
            IClientService clientService,
            IMapper mapper,
            ISalesOrderService salesOrderService,
            IBaseLineService baselineService,
            IOSNotificationService osNotifiService,
            IOrderStatusHistoryService orderStatusHisService
        )
        {
            dataContext = _dataContext;
            _logger = logger;
            _mapper = mapper;
            // Private
            _osOrderItemRepository = new DynamicBaseRepository<OsOrderItem>(dataContext);
            _osOrderRepository = new DynamicBaseRepository<OsOrderInformation>(dataContext);
            _orderInformationsRepository = new DynamicBaseRepository<SO_OrderInformations>(dataContext);
            _settingRepository = new DynamicBaseRepository<SO_SalesOrderSetting>(dataContext);
            _allocationDetailRepo = new DynamicBaseRepository<INV_AllocationDetail>(dataContext);
            _alocationtrackinglogRepo = new DynamicBaseRepository<InvAllocationTracking>(dataContext);
            _odMappingOrderStatusRepo = new DynamicBaseRepository<ODMappingOrderStatus>(dataContext);

            // Public
            _kitRepository = new DynamicBaseRepository<Kit>(dataContext);
            _vatRepository = new DynamicBaseRepository<Vat>(dataContext);

            _inventoryService = inventoryService;
            _clientService = clientService;
            _salesOrderService = salesOrderService;
            _baselineService = baselineService;
            _osNotifiService = osNotifiService;
            _orderStatusHisService = orderStatusHisService;

            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
            _distributorCode = _httpContextAccessor.HttpContext?.Items["DistributorCode"] as string ?? _distributorCode;
        }

        public async Task<BaseResultModel> hamTest()
        {

            for (int i = 0; i < 3; i++)
            {
                // Get allocation detail current
                QueryAllocationModel reqGetRealtimeAllocation = new();
                reqGetRealtimeAllocation.DistributorCode = "p0401241";
                reqGetRealtimeAllocation.WarehouseCode = "S01";
                reqGetRealtimeAllocation.LocationCode = "1";
                reqGetRealtimeAllocation.ItemCode = "2024130301";

                ResultModelWithObject<INV_AllocationDetail> resAllocationDetailCurrent = await _inventoryService.GetAllocationDetailCurrent(reqGetRealtimeAllocation);

                if (!resAllocationDetailCurrent.IsSuccess)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = resAllocationDetailCurrent.Code,
                        Message = resAllocationDetailCurrent.Message
                    };
                }

                INV_AllocationDetail allocationDetailCurrent = resAllocationDetailCurrent.Data;

                BookAllocationModel reqBook = new();
                reqBook.OrderID = _osOrderId;
                reqBook.OneShopID = _oneShopId;
                reqBook.FFAVisitID = null;
                reqBook.CreatedBy = _createdBy;
                reqBook.BookBaseQty = 24;
                reqBook.BookQty = 1;
                reqBook.BookUom = "THUNG";
                reqBook.ItemGroupCode = null;
                reqBook.Priority = 0;

                // Cập nhật Số booked mới
                var resultBooked = await _inventoryService.UpdateBooked(allocationDetailCurrent, reqBook, _listInvTransactionByOneShopId);
                if (!resultBooked.IsSuccess) return resultBooked;
            }
            return new BaseResultModel { IsSuccess = true };
        }

        #region ONE SHOP
        // Get list OneShop order
        public async Task<ResultModelWithObject<ListOsOrderModel>> GetListOrder(SearchOsOrderModel req)
        {
            try
            {
                // Calculate baseline Date
                //var baselineDateCurrent = DateTime.Now;
                //var resultDate = await _baselineService.HandleCalculateBaselineDate();
                //if (!resultDate.IsSuccess)
                //{
                //    return new ResultModelWithObject<ListOsOrderModel>()
                //    {
                //        Code = resultDate.Code,
                //        Message = resultDate.Message,
                //        IsSuccess = false
                //    };
                //}
                //baselineDateCurrent = (DateTime)resultDate.Data;

                // Query OneShop header order
                //var res = _osOrderRepository.GetAllQueryable(null, null, null, _schemaName)
                //    .AsNoTracking()
                //    .Where(header => 
                //        (header.IsDeleted.HasValue && !header.IsDeleted.Value || !header.IsDeleted.HasValue) &&
                //        header.OrderDate.HasValue && header.OrderDate.Value.Date >= baselineDateCurrent.Date);

                var res = _osOrderRepository
                    .GetAllQueryable(null, null, null, _schemaName)
                    .AsNoTracking()
                    .Where(header =>
                        !string.IsNullOrEmpty(header.Status) &&
                        (header.IsDeleted.HasValue &&
                        !header.IsDeleted.Value ||
                        !header.IsDeleted.HasValue));


                // Filter Dynamic
                if (req.Filter != null && req.Filter.Trim() != string.Empty && req.Filter.Trim() != "NA_EMPTY")
                {
                    res = res.Where(DynamicExpressionParser.ParseLambda(new[] { Expression.Parameter(typeof(OsOrderInformation), "s") }, typeof(bool), req.Filter));
                }

                if (req.FilterStatus != null && req.FilterStatus.Count > 0)
                {
                    res = res.Where(x => req.FilterStatus.Contains(x.SOStatus));
                }

                // FromDate, ToDate
                if (req.FromDate.HasValue)
                {
                    res = res.Where(x => x.OrderDate.Value.Date >= req.FromDate.Value.Date);
                }
                if (req.ToDate.HasValue)
                {
                    res = res.Where(x => x.OrderDate.Value.Date <= req.ToDate.Value.Date);
                }

                res = res.OrderByDescending(x => x.OrderDate);

                List<OsOrderModel> listDataRes = new List<OsOrderModel>();

                // Handle total đơn
                int _totalOrder = res.Count();
                IQueryable<OsOrderInformation> req2 = res;
                int _totlaOrderFailed = req2.Where(x => x.SOStatus == SO_SaleOrderStatusConst.OUTOFBUDGET
                                  || x.SOStatus == SO_SaleOrderStatusConst.OUTOFSTOCK
                                  || x.SOStatus == SO_SaleOrderStatusConst.OUTOFSTOCKBUDGET).Count();

                if (req.IsDropdown)
                {
                    listDataRes = _mapper.Map<List<OsOrderModel>>(res.ToList());
                    foreach (var dataRes in listDataRes)
                    {
                        dataRes.OrderItems = await _osOrderItemRepository
                            .GetAllQueryable(x => x.ExternalOrdNbr == dataRes.ExternalOrdNbr, null, null, _schemaName)
                            .ToListAsync();
                    }

                    var reponse = new ListOsOrderModel { Items = listDataRes, TotalOrder = _totalOrder, TotalOrderFailed = _totlaOrderFailed };
                    return new ResultModelWithObject<ListOsOrderModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = reponse
                    };
                }
                else
                {
                    var poTempPagged = PagedList<OsOrderInformation>.ToPagedListQueryAble(res, (req.PageNumber - 1) * req.PageSize, req.PageSize);
                    listDataRes = _mapper.Map<List<OsOrderModel>>(poTempPagged.ToList());
                    foreach (var dataRes in listDataRes)
                    {
                        dataRes.OrderItems = await _osOrderItemRepository
                            .GetAllQueryable(x => x.ExternalOrdNbr == dataRes.ExternalOrdNbr, null, null, _schemaName)
                            .ToListAsync();
                    }

                    var repsonse = new ListOsOrderModel { Items = listDataRes, MetaData = poTempPagged.MetaData, TotalOrder = _totalOrder, TotalOrderFailed = _totlaOrderFailed };
                    return new ResultModelWithObject<ListOsOrderModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = repsonse
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<ListOsOrderModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
        // Get detail OneShop order
        public async Task<ResultModelWithObject<OsOrderModel>> GetDetailOrder(string code)
        {
            try
            {
                OsOrderInformation orderInDb = await _osOrderRepository
                    .GetAllQueryable(x => x.ExternalOrdNbr == code, null, null, _schemaName)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (orderInDb == null)
                {
                    return new ResultModelWithObject<OsOrderModel>
                    {
                        IsSuccess = false,
                        Message = "Cannot found order",
                        Code = 404
                    };
                } 

                OsOrderModel res = _mapper.Map<OsOrderModel>(orderInDb);
                List<OsOrderItem> orderItems = await _osOrderItemRepository
                    .GetAllQueryable(x => x.ExternalOrdNbr == code, null, null, _schemaName)
                    .AsNoTracking()
                    .ToListAsync();

                res.OrderItems = orderItems;
                //dataContext.Attach(res);
                return new ResultModelWithObject<OsOrderModel>
                {
                    IsSuccess = true,
                    Message = "Successfully",
                    Data = res,
                    Code = 200
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<OsOrderModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
        public async Task<BaseResultModel> CheckInvTransactionNegative(List<INV_InventoryTransaction> listTransaction)
        {
            try
            {
                var group = listTransaction.GroupBy(x => new { x.OneShopId, x.ItemCode, x.ItemGroupCode }).Select(x => x.First()).ToList();
                foreach (var item in group)
                {
                    var totalLine = listTransaction.Where(x => x.OneShopId == item.OneShopId && x.ItemCode == item.ItemCode && x.ItemGroupCode == item.ItemGroupCode).ToList();
                    int booked = 0;
                    foreach (var line in totalLine)
                    {
                        booked += line.OrderBaseQuantity.Value;
                    }

                    if (booked < 0)
                    {
                        return new BaseResultModel
                        {
                            IsSuccess = false,
                            Message = $"Error with Order OneShopId {item.OneShopId}. The number of books is negative of item {item.ItemCode}, Group {item.ItemGroupCode}",
                            Code = 400
                        };
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
        // Import list order
        public async Task<BaseResultModel> ImportListOrder(ImportListOSOrder dataInput, string token, string username)
        {
            try
            {
                var listOrder = new List<OsOrderModel>();

                foreach (var orderNumber in dataInput.ExternalOrdNbrs)
                {
                    var duplicated = await _osOrderRepository.GetAllQueryable(x => x.ExternalOrdNbr == orderNumber, null, null, _schemaName).CountAsync();
                    if (duplicated > 1)
                    {
                        return new BaseResultModel()
                        {
                            Code = 400,
                            Message = $"Đơn hàng mã {orderNumber} bị trùng. Vui lòng kiểm tra lại",
                            IsSuccess = false
                        };
                    }
                    var osOrder = await GetDetailOrder(orderNumber);
                    if (!osOrder.IsSuccess)
                    {
                        return new BaseResultModel()
                        {
                            Code = osOrder.Code,
                            Message = osOrder.Message,
                            IsSuccess = false
                        };
                    }

                    if (osOrder.Data.OrderItems.Count == 0)
                    {
                        continue;
                    }

                    if (osOrder.Data.SOStatus != SO_SaleOrderStatusConst.WAITINGIMPORT &&
                        osOrder.Data.SOStatus != SO_SaleOrderStatusConst.OUTOFSTOCK &&
                        osOrder.Data.SOStatus != SO_SaleOrderStatusConst.OUTOFBUDGET &&
                        osOrder.Data.SOStatus != SO_SaleOrderStatusConst.OUTOFSTOCKBUDGET)
                    {
                        return new BaseResultModel()
                        {
                            Code = 400,
                            Message = $"Đơn hàng mã {orderNumber} trạng thái không được phép import",
                            IsSuccess = false
                        };
                    }

                    listOrder.Add(osOrder.Data);
                }

                if (listOrder.Count > 0)
                {
                    // Handle flow budget
                    var resultHandleBudget = await HandleLogicBook(listOrder, token, username);
                    if (!resultHandleBudget.IsSuccess) return resultHandleBudget;
                }
                else
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 404,
                        Message = "order can not found or order has no item"
                    };
                }


                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success"
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
        // Cancel list order
        public async Task<BaseResultModel> CancelListOrder(ImportListOSOrder dataInput, string token, string username, bool isFromOs = true)
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
                Serilog.Log.Information($"############ Start CancelListOrder");
                var listOrder = new List<OsOrderModel>();
                var listOrderHaveSoOrderInformation = new List<OsOrderModel>();

                foreach (var orderNumber in dataInput.ExternalOrdNbrs)
                {
                    var duplicated = await _osOrderRepository.GetAllQueryable(x => x.ExternalOrdNbr == orderNumber, null, null, _schemaName).CountAsync();
                    if (duplicated > 1)
                    {
                        return new BaseResultModel()
                        {
                            Code = 400,
                            Message = $"Đơn hàng mã {orderNumber} bị trùng. Vui lòng kiểm tra lại",
                            IsSuccess = false
                        };
                    }
                    var osOrder = await GetDetailOrder(orderNumber);
                    if (!osOrder.IsSuccess)
                    {
                        return new BaseResultModel()
                        {
                            Code = osOrder.Code,
                            Message = osOrder.Message,
                            IsSuccess = false
                        };
                    }

                    if (osOrder.Data.OrderItems.Count == 0)
                    {
                        continue;
                    }

                    if (!SO_SaleOrderStatusConst.AllowCancelStatuses.Contains(osOrder.Data.SOStatus))
                    {
                        return new BaseResultModel()
                        {
                            Code = 400,
                            Message = $"Đơn hàng mã {orderNumber} trạng thái không được phép hủy",
                            IsSuccess = false
                        };
                    }
                    if (SO_SaleOrderStatusConst.HaveNoSOStatuses.Contains(osOrder.Data.SOStatus))
                    {
                        listOrder.Add(osOrder.Data);
                    }
                    else
                    {
                        listOrderHaveSoOrderInformation.Add(osOrder.Data);
                    }
                }

                if (listOrder.Count > 0)
                {
                    // Handle flow budget
                    var resultHandleBudget = await HandleCancelOSOrder(listOrder, token, username, isFromOs);
                    if (!resultHandleBudget.IsSuccess) return resultHandleBudget;
                }
                
                if (listOrderHaveSoOrderInformation.Count > 0)
                {
                    StringBuilder errorCancelSoMessages = new StringBuilder();
                    foreach (var osOrderInfomation in listOrderHaveSoOrderInformation)
                    {
                        var statusMapping = await _orderStatusHisService.HandleOSMappingStatus(SO_SaleOrderStatusConst.CANCEL, isFromOs);
                        osOrderInfomation.Status = statusMapping.OneShopOrderStatus;
                        osOrderInfomation.SOStatus = SO_SaleOrderStatusConst.CANCEL;
                        _osOrderRepository.UpdateUnSaved(osOrderInfomation, _schemaName);

                        var soOrderInformation = await _salesOrderService.GetDetailSO(new SaleOrderDetailQueryModel
                        {
                            OrderRefNumber = osOrderInfomation.OrderRefNumber,
                            DistributorCode = osOrderInfomation.DistributorCode
                        });
                        if (soOrderInformation?.Data != null)
                        {
                            var cancelResult = await _salesOrderService.CancelNewSO(soOrderInformation?.Data, token, username, isFromOs);
                            if (cancelResult.IsSuccess == false)
                                errorCancelSoMessages.Append($"{osOrderInfomation.ExternalOrdNbr}: {cancelResult.Message}");
                        }
                    }
                    if (errorCancelSoMessages.Length > 0)
                    {
                        return new BaseResultModel
                        {
                            IsSuccess = false,
                            Code = 500,
                            Message = errorCancelSoMessages.ToString()
                        };
                    }
                }

                stopwatch.Start();
                Serilog.Log.Information($"############ End CancelListOrder time: {stopwatch.ElapsedMilliseconds}");

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success"
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

        // Handle book item
        public async Task<BaseResultModel> HandleBookInventory(OsOrderItem item, string distributorCode, string wareHouseCode, string token)
        {
            try
            {
                if (item.UnitRate == 0 && item.Uom != item.BaseUnitCode)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = $"Error with Order {item.ExternalOrdNbr}. Invalid UnitRate of item {item.ItemCode} , Group {item.ItemGroupCode} , type {item.AllocateType}"
                    };
                }

                if (item.OriginalOrderQtyBooked == null) item.OriginalOrderQtyBooked = 0;
                // Số book mong muốn hiện tại
                int _wantedBookBaseQty = item.OriginalOrderBaseQty.HasValue ? item.OriginalOrderBaseQty.Value : 0;
                // Số đã book trước đó
                int _bookedSalesQty = item.OriginalOrderQtyBooked.HasValue ? item.OriginalOrderQtyBooked.Value : 0;

                var remainbaseQty = _wantedBookBaseQty - (int)(_bookedSalesQty * item.UnitRate);
                // Số sales reamin sẽ cần phải book
                var remainNeedBook = (int)(remainbaseQty / item.UnitRate);

                _detailSKUCurrent = null;
                if (item.AllocateType.ToUpper() == AllocateType.SKU.ToUpper() ||
                    item.AllocateType.ToUpper() == AllocateType.KIT.ToUpper())
                {

                    if (item.ItemCode == null || string.IsNullOrEmpty(item.ItemCode))
                    {
                        return new BaseResultModel()
                        {
                            IsSuccess = false,
                            Message = $"Cannot found item code in order code: {item.ExternalOrdNbr}",
                            Code = 404
                        };
                    }

                    // Đã book trước đó
                    if (_bookedSalesQty != 0)
                    {
                        // Handle transaction
                        INV_InventoryTransaction transactionCheck = _listInvTransactionByOneShopId
                            .Where(x => x.ItemCode == item.ItemCode && !x.IsCreateOrderItem)
                            .FirstOrDefault();

                        if (transactionCheck == null)
                        {
                            _stockImport = false;
                            _stockImportAll = false;
                            return new BaseResultModel
                            {
                                IsSuccess = true,
                                Code = 200,
                                Message = "Success"
                            };
                        }
                    }

                    // Handle SKU
                    _detailSKUCurrent = _listInventoryItem.FirstOrDefault(x => x.InventoryItemId == item.ItemCode);
                    // Nếu detail SKU null
                    if (_detailSKUCurrent == null)
                    {
                        // Get detail inventory item
                        _detailSKUCurrent = _clientService.CommonRequest<ExInventoryItemModel>(
                            CommonData.SystemUrlCode.ODItemAPI,
                            $"InventoryItem/ExGetInventoryItemByCode/{item.ItemCode}",
                            Method.GET,
                            $"Rdos {token.Split(" ").Last()}",
                            null);

                        _listInventoryItem.Add(_detailSKUCurrent);
                    }

                    // Đắp data cho item của SOOrderItems
                    _KitId = Guid.Empty;

                    // Lưu id kit
                    if (item.AllocateType.ToUpper() == AllocateType.KIT.ToUpper())
                    {
                        // Get detail KIT
                        var kitInDb = await _kitRepository.GetAllQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.ItemKitId == item.KitId);
                        _KitId = kitInDb != null ? kitInDb.Id : Guid.Empty;
                    }

                    if (remainNeedBook <= 0)
                    {
                        // Không cần book kho nữa
                        // Map detail
                        SO_OrderItems soOrderItem = await MappingSOOrderItems(item, _bookedSalesQty, _wantedBookBaseQty);
                        _listSOOrderItem.Add(soOrderItem);
                    }
                    else
                    {
                        // Get allocation detail current
                        QueryAllocationModel reqGetRealtimeAllocation = new();
                        reqGetRealtimeAllocation.DistributorCode = distributorCode;
                        reqGetRealtimeAllocation.WarehouseCode = wareHouseCode;
                        reqGetRealtimeAllocation.LocationCode = _locationDefaultCurrent.Code.ToString();
                        reqGetRealtimeAllocation.ItemCode = item.ItemCode;

                        ResultModelWithObject<INV_AllocationDetail> resAllocationDetailCurrent = await _inventoryService.GetAllocationDetailCurrent(reqGetRealtimeAllocation);

                        if (!resAllocationDetailCurrent.IsSuccess)
                        {
                            return new BaseResultModel
                            {
                                IsSuccess = false,
                                Code = resAllocationDetailCurrent.Code,
                                Message = resAllocationDetailCurrent.Message
                            };
                        }

                        INV_AllocationDetail allocationDetailCurrent = resAllocationDetailCurrent.Data;


                        // HandleBooked
                        // Số sales qty có thể booked
                        var allocationAllowToBook = (int)(allocationDetailCurrent.Available / item.UnitRate);

                        // check allocation khi quy ra sales phải ít nhất là 1
                        if (allocationAllowToBook > 1)
                        {
                            if (allocationAllowToBook < remainNeedBook)
                            {
                                //đánh 2 cờ Need Confirm
                                _stockImport = false;
                                _stockImportAll = false;
                            }
                            else
                            {
                                //int SalesBookedQty = remainNeedBook;
                                int BaseBookedQty = (int)(remainNeedBook * item.UnitRate);

                                BookAllocationModel reqBook = new();
                                reqBook.OrderID = _osOrderId;
                                reqBook.OneShopID = _oneShopId;
                                reqBook.FFAVisitID = null;
                                reqBook.CreatedBy = _createdBy;
                                reqBook.BookBaseQty = BaseBookedQty;
                                reqBook.BookQty = remainNeedBook;
                                reqBook.BookUom = item.Uom;
                                reqBook.ItemGroupCode = null;
                                reqBook.Priority = 0;

                                // Cập nhật Số booked mới
                                item.OriginalOrderQtyBooked += remainNeedBook;
                                var resultBooked = await _inventoryService.UpdateBooked(allocationDetailCurrent, reqBook, _listInvTransactionByOneShopId);
                                if (!resultBooked.IsSuccess) return resultBooked;

                                // handle cancel transaction
                                INV_InventoryTransaction transaction = _listInvTransactionByOneShopId.FirstOrDefault(x => x.ItemCode == item.ItemCode && item.ItemGroupCode == null);
                                // Nếu item đã được book trước đó thì update lại IsCreateOrderItem = true
                                if (transaction != null)
                                {
                                    transaction.IsCreateOrderItem = true;
                                }

                                // Map detail
                                SO_OrderItems soOrderItem = await MappingSOOrderItems(item, item.OriginalOrderQtyBooked.Value, (int)(item.OriginalOrderQtyBooked.Value * item.UnitRate));
                                _listSOOrderItem.Add(soOrderItem);
                            }
                        }
                        else
                        {
                            //đánh 2 cờ Need Confirm
                            _stockImport = false;
                            _stockImportAll = false;
                        }
                    }
                }
                // Line item is Group or Attribute
                else if (item.AllocateType.ToUpper() == AllocateType.GROUP.ToUpper() ||
                        item.AllocateType.ToUpper() == AllocateType.ATTRIBUTE.ToUpper())
                {
                    // Khởi tạo danh sách các item SKU được tách từ ItemGroup
                    _listSOOrderItem = new List<SO_OrderItems>();

                    // Handle transaction
                    List<INV_InventoryTransaction> listInvTransactionByItemGroup = _listInvTransactionByOneShopId
                        .Where(x => x.ItemGroupCode == item.ItemGroupCode)
                        .OrderBy(x => x.Priority)
                        .ToList();

                    // Đã book trước đó
                    if (_bookedSalesQty != 0)
                    {
                        if (listInvTransactionByItemGroup.Where(x => !x.IsCreateOrderItem).ToList().Count == 0)
                        {
                            _stockImport = false;
                            _stockImportAll = false;
                            return new BaseResultModel
                            {
                                IsSuccess = true,
                                Code = 200,
                                Message = "Success"
                            };
                        }
                    }

                    bool isCheckSuccess = false;

                    if (listInvTransactionByItemGroup.Count > 0)
                    {
                        int count = 0;
                        // Group by transaction nếu có từ 2 line có item code giống nhau trở lên
                        foreach (var tranDetailGroup in listInvTransactionByItemGroup.GroupBy(x => x.ItemCode).Select(x => x.First()).ToList())
                        {
                            // Kiểm tra và book số còn lại cho item có độ ưu tiên cao
                            count += 1;
                            if (remainNeedBook < 1)
                            {
                                isCheckSuccess = true;
                                break;
                            }
                            if (isCheckSuccess) break;

                            // Get allocation detail current
                            QueryAllocationModel reqGetRealtimeAllocation = new();
                            reqGetRealtimeAllocation.DistributorCode = distributorCode;
                            reqGetRealtimeAllocation.WarehouseCode = wareHouseCode;
                            reqGetRealtimeAllocation.LocationCode = _locationDefaultCurrent.Code.ToString();
                            reqGetRealtimeAllocation.ItemCode = tranDetailGroup.ItemCode;

                            ResultModelWithObject<INV_AllocationDetail> resAllocationDetailCurrent = await _inventoryService.GetAllocationDetailCurrent(reqGetRealtimeAllocation);

                            if (!resAllocationDetailCurrent.IsSuccess)
                            {
                                return new BaseResultModel
                                {
                                    IsSuccess = false,
                                    Code = resAllocationDetailCurrent.Code,
                                    Message = resAllocationDetailCurrent.Message
                                };
                            }

                            INV_AllocationDetail allocationDetailCurrent = resAllocationDetailCurrent.Data;

                            // Số sales qty có thể booked
                            var allocationAllowToBook = (int)(allocationDetailCurrent.Available / item.UnitRate);

                            // check allocation khi quy ra sales phải ít nhất là 1
                            if (allocationAllowToBook > 1)
                            {
                                if (allocationAllowToBook < remainNeedBook)
                                {
                                    //_stockImport = false;
                                    //_stockImportAll = false;
                                    continue;
                                }
                                else
                                {
                                    int BaseBookedQty = (int)(remainNeedBook * item.UnitRate);

                                    // Handle SKU
                                    _detailSKUCurrent = _listInventoryItem.FirstOrDefault(x => x.InventoryItemId == tranDetailGroup.ItemCode);
                                    // Nếu detail SKU null
                                    if (_detailSKUCurrent == null)
                                    {
                                        // Get detail inventory item
                                        _detailSKUCurrent = _clientService.CommonRequest<ExInventoryItemModel>(
                                            CommonData.SystemUrlCode.ODItemAPI,
                                            $"InventoryItem/ExGetInventoryItemByCode/{tranDetailGroup.ItemCode}",
                                            Method.GET,
                                            $"Rdos {token.Split(" ").Last()}",
                                            null);

                                        _listInventoryItem.Add(_detailSKUCurrent);
                                    }

                                    // Không tìm thấy data detail thì skip
                                    if (_detailSKUCurrent == null)
                                    {
                                        //đánh 2 cờ Need Confirm
                                        _stockImport = false;
                                        _stockImportAll = false;
                                        continue;
                                    }

                                    // Handle book kho
                                    BookAllocationModel reqBook = new();
                                    reqBook.OrderID = _osOrderId;
                                    reqBook.OneShopID = _oneShopId;
                                    reqBook.FFAVisitID = null;
                                    reqBook.CreatedBy = _createdBy;
                                    reqBook.BookBaseQty = BaseBookedQty;
                                    reqBook.BookQty = remainNeedBook;
                                    reqBook.BookUom = item.Uom;
                                    reqBook.ItemGroupCode = item.ItemGroupCode;
                                    reqBook.Priority = count;

                                    // Cập nhật Số booked mới
                                    item.OriginalOrderQtyBooked += remainNeedBook;
                                    var resultBooked = await _inventoryService.UpdateBooked(allocationDetailCurrent, reqBook, _listInvTransactionByOneShopId);
                                    if (!resultBooked.IsSuccess) return resultBooked;

                                    // Nếu item đã được book trước đó thì tạo thành item của đơn SO phải + lại
                                    int _BookedQty = remainNeedBook;
                                    int _BookedBaseQty = BaseBookedQty;
                                    foreach (var tranDetail in listInvTransactionByItemGroup.Where(x => x.ItemCode == _detailSKUCurrent.InventoryItemId).ToList())
                                    {
                                        _BookedQty += tranDetail.Quantity;
                                        _BookedBaseQty += tranDetail.BaseQuantity;
                                        tranDetail.IsCreateOrderItem = true;
                                    }

                                    var itemNew = await MappingSOOrderItems(item, _BookedQty, _BookedBaseQty);
                                    _listSOOrderItem.Add(itemNew);
                                    isCheckSuccess = true;
                                    tranDetailGroup.IsCreateOrderItem = true;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }

                    if (!isCheckSuccess) // Trường họp không đủ hoặc chưa book
                    {
                        // Get item SKU và Số lượng cần book hiện tại
                        var standarBookRequest = new StandardRequestModel()
                        {
                            DistributorCode = distributorCode,
                            DistributorShiptoCode = wareHouseCode,
                            ItemGroupCode = item.ItemGroupCode,
                            Quantity = remainbaseQty,
                        };

                        var standardBookSkus = _clientService.CommonRequest<List<StandardItemModel>>(
                            CommonData.SystemUrlCode.ODItemAPI,
                            $"Standard/GetStockInventoryItemStdByItemGroupVer2",
                            Method.POST,
                            $"Rdos {token.Split(" ").Last()}",
                            standarBookRequest
                           );

                        // Tổng Số booked mới hiện tại của các item SKU 
                        if (standardBookSkus != null && standardBookSkus.Count > 0)
                        {
                            if (standardBookSkus.Any(x => x.StdRuleCode == PriorityStandard.Ratio))
                            {
                                standardBookSkus = standardBookSkus.OrderByDescending(x => x.Ratio).ToList();
                            }
                            else
                            {
                                standardBookSkus = standardBookSkus.OrderBy(x => x.Priority).ToList();
                            }

                            int count = 0;
                            // Loop SKU
                            foreach (var skuStandar in standardBookSkus)
                            {
                                count += 1;

                                //Nếu remain không còn => break
                                if (remainNeedBook < 1) break;

                                //Sales Qty của sku
                                int skuSalesAvailableQty = 0;
                                if (count == 1)
                                {
                                    // Nếu là SKU ưu tiên cao thì quy đổi làm tròn lên
                                    skuSalesAvailableQty = (int)Math.Ceiling((double)skuStandar.Avaiable / (double)item.UnitRate);
                                }
                                else
                                {
                                    skuSalesAvailableQty = (int)(skuStandar.Avaiable / item.UnitRate);
                                    if (skuSalesAvailableQty < remainNeedBook)
                                    {
                                        skuSalesAvailableQty = (int)Math.Ceiling((double)skuStandar.Avaiable / (double)item.UnitRate);
                                    }
                                }
                                //Avalable không đủ theo sales - skip
                                if (skuSalesAvailableQty < 1) continue;

                                // Get allocation detail current
                                QueryAllocationModel reqGetRealtimeAllocation = new();
                                reqGetRealtimeAllocation.DistributorCode = distributorCode;
                                reqGetRealtimeAllocation.WarehouseCode = wareHouseCode;
                                reqGetRealtimeAllocation.LocationCode = _locationDefaultCurrent.Code.ToString();
                                reqGetRealtimeAllocation.ItemCode = skuStandar.InventoryCode;

                                ResultModelWithObject<INV_AllocationDetail> resAllocationDetailCurrent = await _inventoryService.GetAllocationDetailCurrent(reqGetRealtimeAllocation);

                                if (!resAllocationDetailCurrent.IsSuccess)
                                {
                                    return new BaseResultModel
                                    {
                                        IsSuccess = false,
                                        Code = resAllocationDetailCurrent.Code,
                                        Message = resAllocationDetailCurrent.Message
                                    };
                                }

                                INV_AllocationDetail allocationDetailCurrent = resAllocationDetailCurrent.Data;

                                // Số sales qty có thể booked
                                var allocationAllowToBook = (int)(allocationDetailCurrent.Available / item.UnitRate);

                                // check allocation khi quy ra sales phải ít nhất là 1
                                if (allocationAllowToBook > 1)
                                {
                                    if (allocationAllowToBook < skuSalesAvailableQty)
                                    {
                                        _stockImport = false;
                                        _stockImportAll = false;
                                        continue;
                                    }
                                    else
                                    {
                                        //int SalesBookedQty = allocationAllowToBook > skuSaleQtyNeedBook ? skuSaleQtyNeedBook : allocationAllowToBook;
                                        int BaseBookedQty = (int)(skuSalesAvailableQty * item.UnitRate);

                                        if (skuSalesAvailableQty > 0)
                                        {
                                            // Handle SKU
                                            _detailSKUCurrent = _listInventoryItem.FirstOrDefault(x => x.InventoryItemId == skuStandar.InventoryCode);
                                            // Nếu detail SKU null
                                            if (_detailSKUCurrent == null)
                                            {
                                                // Get detail inventory item
                                                _detailSKUCurrent = _clientService.CommonRequest<ExInventoryItemModel>(
                                                    CommonData.SystemUrlCode.ODItemAPI,
                                                    $"InventoryItem/ExGetInventoryItemByCode/{skuStandar.InventoryCode}",
                                                    Method.GET,
                                                    $"Rdos {token.Split(" ").Last()}",
                                                    null);

                                                _listInventoryItem.Add(_detailSKUCurrent);
                                            }

                                            // Không tìm thấy data detail thì skip
                                            if (_detailSKUCurrent == null)
                                            {
                                                //đánh 2 cờ Need Confirm
                                                _stockImport = false;
                                                _stockImportAll = false;
                                                continue;
                                            }

                                            // Handle book kho
                                            BookAllocationModel reqBook = new();
                                            reqBook.OrderID = _osOrderId;
                                            reqBook.OneShopID = _oneShopId;
                                            reqBook.FFAVisitID = null;
                                            reqBook.CreatedBy = _createdBy;
                                            reqBook.BookBaseQty = BaseBookedQty;
                                            reqBook.BookQty = skuSalesAvailableQty;
                                            reqBook.BookUom = item.Uom;
                                            reqBook.ItemGroupCode = item.ItemGroupCode;
                                            reqBook.Priority = count;

                                            // Cập nhật Số booked mới
                                            item.OriginalOrderQtyBooked += skuSalesAvailableQty;
                                            var resultBooked = await _inventoryService.UpdateBooked(allocationDetailCurrent, reqBook, _listInvTransactionByOneShopId);
                                            if (!resultBooked.IsSuccess) return resultBooked;

                                            int _BookedQty = skuSalesAvailableQty;
                                            int _BookedBaseQty = BaseBookedQty;
                                            // Handle lại total qty nếu có transaction trước đó
                                            foreach (var tranDetail in listInvTransactionByItemGroup.Where(x => x.ItemCode == _detailSKUCurrent.InventoryItemId).ToList())
                                            {
                                                _BookedQty += tranDetail.Quantity;
                                                _BookedBaseQty += tranDetail.BaseQuantity;
                                                tranDetail.IsCreateOrderItem = true;
                                            }

                                            var itemNew = await MappingSOOrderItems(item, _BookedQty, _BookedBaseQty);
                                            _listSOOrderItem.Add(itemNew);

                                            remainNeedBook -= skuSalesAvailableQty;
                                        }
                                    }
                                }
                                else
                                {
                                    _stockImport = false;
                                    _stockImportAll = false;
                                    continue;
                                }
                            }

                            if (remainNeedBook > 0)
                            {
                                _stockImport = false;
                                _stockImportAll = false;
                                _listSOOrderItem = new List<SO_OrderItems>();
                            }
                        }
                        else
                        {
                            _stockImport = false;
                            _stockImportAll = false;
                        }
                    }
                }

                if (_stockImport)
                {
                    // List transaction chưa map thành item trong SO Order
                    List<INV_InventoryTransaction> _listHandleCreateOrderItem = _listInvTransactionByOneShopId.Where(x => x.ItemGroupCode != null && x.ItemGroupCode == item.ItemGroupCode && !x.IsCreateOrderItem).ToList();
                    foreach (var itemInvGroup in _listHandleCreateOrderItem.GroupBy(x => x.ItemCode).Select(x => x.First()).ToList())
                    {
                        // Handle SKU
                        _detailSKUCurrent = _listInventoryItem.FirstOrDefault(x => x.InventoryItemId == itemInvGroup.ItemCode);
                        // Nếu detail SKU null
                        if (_detailSKUCurrent == null)
                        {
                            // Get detail inventory item
                            _detailSKUCurrent = _clientService.CommonRequest<ExInventoryItemModel>(
                                CommonData.SystemUrlCode.ODItemAPI,
                                $"InventoryItem/ExGetInventoryItemByCode/{itemInvGroup.ItemCode}",
                                Method.GET,
                                $"Rdos {token.Split(" ").Last()}",
                                null);

                            _listInventoryItem.Add(_detailSKUCurrent);
                        }

                        if (_detailSKUCurrent == null) continue;

                        int _Quantity = 0;
                        int _BaseQty = 0;
                        foreach (var itemInv in _listHandleCreateOrderItem.Where(x => x.ItemCode == itemInvGroup.ItemCode).ToList())
                        {
                            _Quantity += itemInv.Quantity;
                            _BaseQty += itemInv.BaseQuantity;
                        }

                        var itemNew = await MappingSOOrderItems(item, _Quantity, _BaseQty);
                        _listSOOrderItem.Add(itemNew);
                    }
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success"
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

        public async Task<BaseResultModel> HandleCancelInventory(OsOrderItem item, string distributorCode, string wareHouseCode, string token)
        {
            try
            {
                // Kiểm tra đơn vị quy đổi
                if (item.UnitRate == 0)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Message = $"Error with Order {item.ExternalOrdNbr}. Invalid UnitRate of item {item.ItemCode} , Group {item.ItemGroupCode} , type {item.AllocateType}"
                    };
                }

                // Số đã book nếu chưa có sẽ bằng 0
                if (item.OriginalOrderQtyBooked == null) item.OriginalOrderQtyBooked = 0;
                // Số book mong muốn hiện tại
                int _wantedBookBaseQty = item.OriginalOrderBaseQty.HasValue ? item.OriginalOrderBaseQty.Value : 0;
                // Số đã book trước đó
                int _bookedSalesQty = item.OriginalOrderQtyBooked.HasValue ? item.OriginalOrderQtyBooked.Value : 0;
                // Số cần book base còn lại
                var remainbaseQty = _wantedBookBaseQty - (int)(_bookedSalesQty * item.UnitRate);
                // Số cần book còn lại theo UOM
                var remainNeedBook = (int)(remainbaseQty / item.UnitRate);

                // Khai báo thông tin SKU hiện tại của flow check
                _detailSKUCurrent = null;
                if (item.AllocateType.ToUpper() == AllocateType.SKU.ToUpper() ||
                    item.AllocateType.ToUpper() == AllocateType.KIT.ToUpper())
                {
                    if (item.ItemCode == null || string.IsNullOrEmpty(item.ItemCode))
                    {
                        return new BaseResultModel()
                        {
                            IsSuccess = false,
                            Message = $"Cannot found item code in order code: {item.ExternalOrdNbr}",
                            Code = 404
                        };
                    }

                    // Handle SKU
                    _detailSKUCurrent = _listInventoryItem.FirstOrDefault(x => x.InventoryItemId == item.ItemCode);
                    // Nếu detail SKU null
                    if (_detailSKUCurrent == null)
                    {
                        // Get detail inventory item
                        _detailSKUCurrent = _clientService.CommonRequest<ExInventoryItemModel>(
                            CommonData.SystemUrlCode.ODItemAPI,
                            $"InventoryItem/ExGetInventoryItemByCode/{item.ItemCode}",
                            Method.GET,
                            $"Rdos {token.Split(" ").Last()}",
                            null);

                        _listInventoryItem.Add(_detailSKUCurrent);
                    }

                    // Đắp data cho item của SOOrderItems
                    _KitId = Guid.Empty;

                    // Lưu id kit
                    if (item.AllocateType.ToUpper() == AllocateType.KIT.ToUpper())
                    {
                        // Get detail KIT
                        var kitInDb = await _kitRepository.GetAllQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.ItemKitId == item.KitId);
                        _KitId = kitInDb != null ? kitInDb.Id : Guid.Empty;
                    }

                    // Get allocation detail current
                    QueryAllocationModel reqGetRealtimeAllocation = new();
                    reqGetRealtimeAllocation.DistributorCode = distributorCode;
                    reqGetRealtimeAllocation.WarehouseCode = wareHouseCode;
                    reqGetRealtimeAllocation.LocationCode = _locationDefaultCurrent.Code.ToString();
                    reqGetRealtimeAllocation.ItemCode = item.ItemCode;

                    ResultModelWithObject<INV_AllocationDetail> resAllocationDetailCurrent = await _inventoryService.GetAllocationDetailCurrent(reqGetRealtimeAllocation);

                    if (!resAllocationDetailCurrent.IsSuccess)
                    {
                        return new BaseResultModel
                        {
                            IsSuccess = false,
                            Code = resAllocationDetailCurrent.Code,
                            Message = resAllocationDetailCurrent.Message
                        };
                    }

                    INV_AllocationDetail allocationDetailCurrent = resAllocationDetailCurrent.Data;

                    // handle cancel transaction
                    INV_InventoryTransaction transaction = _listInvTransactionByOneShopId.FirstOrDefault(x => x.ItemCode == item.ItemCode && item.ItemGroupCode == null);
                    if (transaction != null)
                    {
                        // Handle cancel book kho
                        BookAllocationModel reqBook = new();
                        reqBook.OrderID = _osOrderId;
                        reqBook.OneShopID = _oneShopId;
                        reqBook.FFAVisitID = null;
                        reqBook.CreatedBy = _createdBy;
                        reqBook.BookBaseQty = transaction.BaseQuantity;
                        reqBook.BookQty = transaction.Quantity;
                        reqBook.BookUom = item.Uom;
                        reqBook.ItemGroupCode = null;
                        reqBook.Priority = 0;
                        transaction.IsCreateOrderItem = true;

                        var resCancelBook = await _inventoryService.CancelBooked(allocationDetailCurrent, reqBook, _listInvTransactionByOneShopId);
                        if (!resCancelBook.IsSuccess) return resCancelBook;
                    }

                    // Cập nhật Số booked mới
                    item.OriginalOrderQtyBooked = 0;
                    // Map detail
                    SO_OrderItems soOrderItem = await MappingSOOrderItems(item, (int)(_wantedBookBaseQty / item.UnitRate), _wantedBookBaseQty);
                    _listSOOrderItem.Add(soOrderItem);
                }
                // Line item is Group or Attribute
                else if (item.AllocateType.ToUpper() == AllocateType.GROUP.ToUpper() ||
                        item.AllocateType.ToUpper() == AllocateType.ATTRIBUTE.ToUpper())
                {
                    // Khởi tạo danh sách các item SKU được tách từ ItemGroup
                    _listSOOrderItem = new List<SO_OrderItems>();

                    // Handle transaction
                    List<INV_InventoryTransaction> listInvTransactionByItemGroup = _listInvTransactionByOneShopId
                        .Where(x => x.ItemGroupCode == item.ItemGroupCode)
                        .OrderBy(x => x.Priority)
                        .ToList();

                    // Cập nhật Số booked mới
                    item.OriginalOrderQtyBooked = 0;

                    bool isCheckSuccess = false;
                    if (listInvTransactionByItemGroup.Count > 0)
                    {
                        int count = 0;
                        // Group by transaction nếu có từ 2 line có item code giống nhau trở lên
                        foreach (var tranDetailGroup in listInvTransactionByItemGroup.GroupBy(x => x.ItemCode).Select(x => x.First()).ToList())
                        {
                            // Kiểm tra và book số còn lại cho item có độ ưu tiên cao
                            count += 1;
                            if (remainNeedBook < 1) break;
                            if (isCheckSuccess) break;

                            // Get allocation detail current
                            QueryAllocationModel reqGetRealtimeAllocation = new();
                            reqGetRealtimeAllocation.DistributorCode = distributorCode;
                            reqGetRealtimeAllocation.WarehouseCode = wareHouseCode;
                            reqGetRealtimeAllocation.LocationCode = _locationDefaultCurrent.Code.ToString();
                            reqGetRealtimeAllocation.ItemCode = tranDetailGroup.ItemCode;

                            ResultModelWithObject<INV_AllocationDetail> resAllocationDetailCurrent = await _inventoryService.GetAllocationDetailCurrent(reqGetRealtimeAllocation);

                            if (!resAllocationDetailCurrent.IsSuccess)
                            {
                                return new BaseResultModel
                                {
                                    IsSuccess = false,
                                    Code = resAllocationDetailCurrent.Code,
                                    Message = resAllocationDetailCurrent.Message
                                };
                            }

                            INV_AllocationDetail allocationDetailCurrent = resAllocationDetailCurrent.Data;

                            // Số sales qty có thể booked
                            var allocationAllowToBook = (int)(allocationDetailCurrent.Available / item.UnitRate);

                            // check allocation khi quy ra sales phải ít nhất là 1
                            if (allocationAllowToBook > 1)
                            {
                                if (allocationAllowToBook < remainNeedBook)
                                {
                                    continue;
                                }
                                else
                                {
                                    int BaseBookedQty = (int)(remainNeedBook * item.UnitRate);

                                    // Handle SKU
                                    _detailSKUCurrent = _listInventoryItem.FirstOrDefault(x => x.InventoryItemId == tranDetailGroup.ItemCode);
                                    // Nếu detail SKU null
                                    if (_detailSKUCurrent == null)
                                    {
                                        // Get detail inventory item
                                        _detailSKUCurrent = _clientService.CommonRequest<ExInventoryItemModel>(
                                            CommonData.SystemUrlCode.ODItemAPI,
                                            $"InventoryItem/ExGetInventoryItemByCode/{tranDetailGroup.ItemCode}",
                                            Method.GET,
                                            $"Rdos {token.Split(" ").Last()}",
                                            null);

                                        _listInventoryItem.Add(_detailSKUCurrent);
                                    }

                                    // Không tìm thấy data detail thì skip
                                    if (_detailSKUCurrent == null)
                                    {
                                        continue;
                                    }

                                    // Nếu item đã được book trước đó thì tạo thành item của đơn SO phải + lại
                                    int _BookedQty = remainNeedBook;
                                    int _BookedBaseQty = BaseBookedQty;

                                    // Số book cần trả lại khi đã book trước đó theo transaction
                                    int _cancelBookedQty = 0;
                                    int _cancelBookedBaseQty = 0;
                                    foreach (var tranDetail in listInvTransactionByItemGroup.Where(x => x.ItemCode == _detailSKUCurrent.InventoryItemId).ToList())
                                    {
                                        // Book total
                                        _BookedQty += tranDetail.Quantity;
                                        _BookedBaseQty += tranDetail.BaseQuantity;
                                        // Book canncel
                                        _cancelBookedQty += tranDetail.Quantity;
                                        _cancelBookedBaseQty += tranDetail.BaseQuantity;
                                        tranDetail.IsCreateOrderItem = true;
                                    }

                                    var itemNew = await MappingSOOrderItems(item, _BookedQty, _BookedBaseQty);
                                    _listSOOrderItem.Add(itemNew);
                                    isCheckSuccess = true;

                                    if (_cancelBookedBaseQty > 0 && _cancelBookedQty > 0)
                                    {
                                        // Handle cancel book kho
                                        BookAllocationModel reqBook = new();
                                        reqBook.OrderID = _osOrderId;
                                        reqBook.OneShopID = _oneShopId;
                                        reqBook.FFAVisitID = null;
                                        reqBook.CreatedBy = _createdBy;
                                        reqBook.BookBaseQty = _cancelBookedBaseQty;
                                        reqBook.BookQty = _cancelBookedQty;
                                        reqBook.BookUom = item.Uom;
                                        reqBook.ItemGroupCode = item.ItemGroupCode;
                                        reqBook.Priority = count;

                                        var resCancelBook = await _inventoryService.CancelBooked(allocationDetailCurrent, reqBook, _listInvTransactionByOneShopId);
                                        if (!resCancelBook.IsSuccess) return resCancelBook;
                                    }
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                    }

                    if (!isCheckSuccess) // Trường hợp không đủ hoặc chưa book
                    {
                        // Phân rã item group thành SKU và không cần check available Stock
                        var standardBookSkus = _clientService.CommonRequest<List<StandardItemModel>>(
                            CommonData.SystemUrlCode.ODItemAPI,
                            $"Standard/GetInventoryItemStdByItemGroupByQuantity/{item.ItemGroupCode}/{remainbaseQty}",
                            Method.GET,
                            $"Rdos {token.Split(" ").Last()}",
                            null
                           );

                        // Tổng Số booked mới hiện tại của các item SKU 
                        if (standardBookSkus != null && standardBookSkus.Count > 0)
                        {
                            if (standardBookSkus.Any(x => x.StdRuleCode == PriorityStandard.Ratio))
                            {
                                standardBookSkus = standardBookSkus.OrderByDescending(x => x.Ratio).ToList();
                            }
                            else
                            {
                                standardBookSkus = standardBookSkus.OrderBy(x => x.Priority).ToList();
                            }

                            int count = 0;
                            // Loop SKU
                            foreach (var skuStandar in standardBookSkus)
                            {
                                count += 1;

                                if (remainNeedBook == 0) break;

                                //Sales Qty của sku
                                int skuSalesAvailableQty = 0;
                                if (count == 1)
                                {
                                    // Nếu là SKU ưu tiên cao thì quy đổi làm tròn lên
                                    skuSalesAvailableQty = (int)Math.Ceiling((double)skuStandar.Avaiable / (double)item.UnitRate);
                                }
                                else
                                {
                                    skuSalesAvailableQty = (int)(skuStandar.Avaiable / item.UnitRate);
                                    if (skuSalesAvailableQty < remainNeedBook)
                                    {
                                        skuSalesAvailableQty = (int)Math.Ceiling((double)skuStandar.Avaiable / (double)item.UnitRate);
                                    }
                                }

                                // Avalable không đủ theo sales - skip
                                if (skuSalesAvailableQty < 1) continue;

                                // Get allocation detail current
                                QueryAllocationModel reqGetRealtimeAllocation = new();
                                reqGetRealtimeAllocation.DistributorCode = distributorCode;
                                reqGetRealtimeAllocation.WarehouseCode = wareHouseCode;
                                reqGetRealtimeAllocation.LocationCode = _locationDefaultCurrent.Code.ToString();
                                reqGetRealtimeAllocation.ItemCode = skuStandar.InventoryCode;

                                ResultModelWithObject<INV_AllocationDetail> resAllocationDetailCurrent = await _inventoryService.GetAllocationDetailCurrent(reqGetRealtimeAllocation);

                                if (!resAllocationDetailCurrent.IsSuccess)
                                {
                                    return new BaseResultModel
                                    {
                                        IsSuccess = false,
                                        Code = resAllocationDetailCurrent.Code,
                                        Message = resAllocationDetailCurrent.Message
                                    };
                                }

                                INV_AllocationDetail allocationDetailCurrent = resAllocationDetailCurrent.Data;

                                int BaseBookedQty = (int)(skuSalesAvailableQty * item.UnitRate);

                                // Handle SKU
                                _detailSKUCurrent = _listInventoryItem.FirstOrDefault(x => x.InventoryItemId == skuStandar.InventoryCode);
                                // Nếu detail SKU null
                                if (_detailSKUCurrent == null)
                                {
                                    // Get detail inventory item
                                    _detailSKUCurrent = _clientService.CommonRequest<ExInventoryItemModel>(
                                        CommonData.SystemUrlCode.ODItemAPI,
                                        $"InventoryItem/ExGetInventoryItemByCode/{skuStandar.InventoryCode}",
                                        Method.GET,
                                        $"Rdos {token.Split(" ").Last()}",
                                        null);

                                    _listInventoryItem.Add(_detailSKUCurrent);
                                }

                                // Không tìm thấy data detail thì skip
                                if (_detailSKUCurrent == null)
                                {
                                    continue;
                                }

                                // Tổng số đã book hiện tại
                                int _BookedQty = skuSalesAvailableQty;
                                int _BookedBaseQty = BaseBookedQty;

                                // Số book cần phải trả lại kho theo transaction
                                int _cancelBookedQty = 0;
                                int _cancelBookedBaseQty = 0;

                                // Handle lại total qty nếu có transaction trước đó
                                foreach (var tranDetail in listInvTransactionByItemGroup.Where(x => x.ItemCode == _detailSKUCurrent.InventoryItemId).ToList())
                                {
                                    _BookedQty += tranDetail.Quantity;
                                    _BookedBaseQty += tranDetail.BaseQuantity;
                                    // Book canncel
                                    _cancelBookedQty += tranDetail.Quantity;
                                    _cancelBookedBaseQty += tranDetail.BaseQuantity;
                                    tranDetail.IsCreateOrderItem = true;
                                }

                                var itemNew = await MappingSOOrderItems(item, _BookedQty, _BookedBaseQty);
                                _listSOOrderItem.Add(itemNew);

                                // Trừ số book đã xử lý
                                remainNeedBook -= skuSalesAvailableQty;

                                // Cancel số book đã book trước đó từ transaction
                                if (_cancelBookedBaseQty > 0 && _cancelBookedQty > 0)
                                {
                                    // Handle cancel book kho
                                    BookAllocationModel reqBook = new();
                                    reqBook.OrderID = _osOrderId;
                                    reqBook.OneShopID = _oneShopId;
                                    reqBook.FFAVisitID = null;
                                    reqBook.CreatedBy = _createdBy;
                                    reqBook.BookBaseQty = _cancelBookedBaseQty;
                                    reqBook.BookQty = _cancelBookedQty;
                                    reqBook.BookUom = item.Uom;
                                    reqBook.ItemGroupCode = item.ItemGroupCode;
                                    reqBook.Priority = count;

                                    var resCancelBook = await _inventoryService.CancelBooked(allocationDetailCurrent, reqBook, _listInvTransactionByOneShopId);
                                    if (!resCancelBook.IsSuccess) return resCancelBook;
                                }
                            }
                        }
                    }
                }

                if (_stockImport)
                {
                    // List transaction chưa map thành item trong SO Order
                    List<INV_InventoryTransaction> _listHandleCreateOrderItem = _listInvTransactionByOneShopId.Where(x => x.ItemGroupCode == item.ItemGroupCode && !x.IsCreateOrderItem).ToList();
                    foreach (var itemInvGroup in _listHandleCreateOrderItem.GroupBy(x => x.ItemCode).Select(x => x.First()).ToList())
                    {
                        // Handle SKU
                        _detailSKUCurrent = _listInventoryItem.FirstOrDefault(x => x.InventoryItemId == itemInvGroup.ItemCode);
                        // Nếu detail SKU null
                        if (_detailSKUCurrent == null)
                        {
                            // Get detail inventory item
                            _detailSKUCurrent = _clientService.CommonRequest<ExInventoryItemModel>(
                                CommonData.SystemUrlCode.ODItemAPI,
                                $"InventoryItem/ExGetInventoryItemByCode/{itemInvGroup.ItemCode}",
                                Method.GET,
                                $"Rdos {token.Split(" ").Last()}",
                                null);

                            _listInventoryItem.Add(_detailSKUCurrent);
                        }

                        if (_detailSKUCurrent == null) continue;

                        int _Quantity = 0;
                        int _BaseQty = 0;
                        foreach (var itemInv in _listHandleCreateOrderItem.Where(x => x.ItemCode == itemInvGroup.ItemCode).ToList())
                        {
                            _Quantity += itemInv.Quantity;
                            _BaseQty += itemInv.BaseQuantity;
                        }

                        var itemNew = await MappingSOOrderItems(item, _Quantity, _BaseQty);
                        _listSOOrderItem.Add(itemNew);

                        // Cancel số book đã book trước đó từ transaction
                        if (_Quantity > 0 && _BaseQty > 0)
                        {
                            // Get allocation detail current
                            QueryAllocationModel reqGetRealtimeAllocation = new();
                            reqGetRealtimeAllocation.DistributorCode = distributorCode;
                            reqGetRealtimeAllocation.WarehouseCode = wareHouseCode;
                            reqGetRealtimeAllocation.LocationCode = _locationDefaultCurrent.Code.ToString();
                            reqGetRealtimeAllocation.ItemCode = itemInvGroup.ItemCode;

                            ResultModelWithObject<INV_AllocationDetail> resAllocationDetailCurrent = await _inventoryService.GetAllocationDetailCurrent(reqGetRealtimeAllocation);

                            if (!resAllocationDetailCurrent.IsSuccess)
                            {
                                return new BaseResultModel
                                {
                                    IsSuccess = false,
                                    Code = resAllocationDetailCurrent.Code,
                                    Message = resAllocationDetailCurrent.Message
                                };
                            }

                            INV_AllocationDetail allocationDetailCurrent = resAllocationDetailCurrent.Data;

                            // Handle cancel book kho
                            BookAllocationModel reqBook = new();
                            reqBook.OrderID = _osOrderId;
                            reqBook.OneShopID = _oneShopId;
                            reqBook.FFAVisitID = null;
                            reqBook.CreatedBy = _createdBy;
                            reqBook.BookBaseQty = _BaseQty;
                            reqBook.BookQty = _Quantity;
                            reqBook.BookUom = item.Uom;
                            reqBook.ItemGroupCode = item.ItemGroupCode;
                            reqBook.Priority = itemInvGroup.Priority;

                            var resCancelBook = await _inventoryService.CancelBooked(allocationDetailCurrent, reqBook, _listInvTransactionByOneShopId);
                            if (!resCancelBook.IsSuccess) return resCancelBook;
                        }
                    }
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success"
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

        // Calculate book budget
        public async Task<BaseResultModel> HandleLogicBook(List<OsOrderModel> listInput, string token, string username)
        {
            try
            {
                List<ODMappingOrderStatus> listODMappingStatus = await _odMappingOrderStatusRepo.GetAllQueryable().ToListAsync();
                _createdBy = username;
                // Get list location
                ResultModelWithObject<List<PrincipalWarehouseLocation>> principalLocationList = await _inventoryService.GetListPrincipalWarehouseLocation();
                if (principalLocationList.IsSuccess &&
                    principalLocationList.Data != null &&
                    principalLocationList.Data.Count > 0)
                {
                    _locationDefaultCurrent = principalLocationList.Data.FirstOrDefault(x => x.Code == LocationCodeAllowBoook);
                }
                else
                {
                    return new BaseResultModel()
                    {
                        Code = principalLocationList.Code,
                        Message = principalLocationList.Message,
                        IsSuccess = false
                    };
                }

                #region SalePeriod
                BaseResultModel resGetPeriod = await _salesOrderService.GetPeriodID(listInput.First().OrderDate.Value, token);

                if (!resGetPeriod.IsSuccess)
                {
                    return resGetPeriod;
                }

                _periodID = resGetPeriod?.Data?.ToString();
                #endregion

                // Get list VAT
                _listVats = await _vatRepository.GetAllQueryable().AsNoTracking().ToListAsync();

                // Handle Distributor
                string distributorCode = listInput.First().DistributorCode;

                // Handle distributor shipto
                var resGetShiptoActive = _clientService.CommonRequest<ResultModelWithObject<List<DisShipto>>>(
                    CommonData.SystemUrlCode.ODDistributorAPI,
                    $"DistributorShipto/ExGetListShiptoActiveByDistributorCode/{_distributorCode}",
                    Method.GET,
                    $"Rdos {token.Split(" ").Last()}",
                    null);

                if (!resGetShiptoActive.IsSuccess || resGetShiptoActive.Data.Count == 0)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 404,
                        Message = "Cannot found warehouse"
                    };
                }

                _warehouseCode = resGetShiptoActive.Data.First().ShiptoCode;

                var resDisInfo = _clientService.CommonRequest<ResultModelWithObject<DistributorInfoModel>>(
                    CommonData.SystemUrlCode.SalesOrgAPI,
                    $"DistributorSellingArea/GetInformationDistributor/{distributorCode}",
                    Method.GET,
                    token,
                    null
                   );

                if (resDisInfo == null || resDisInfo.Data == null)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 404,
                        Message = "Distributor not found in saleOrg"
                    };
                }

                _distributorInfo = resDisInfo.Data;
                _distributorInfo.DistributorName = resGetShiptoActive.Data.First().Name;

                // MDM SaleOrg
                var mdmResult = _clientService.CommonRequest<ResultModelWithObject<List<MdmModel>>>(
                    CommonData.SystemUrlCode.SalesOrgAPI,
                    $"DistributorSellingArea/GetListEmployeeManager/{_distributorInfo.DSACode}",
                    RestSharp.Method.GET,
                    token,
                    null
                   );

                _mdmDistributor = mdmResult.IsSuccess ? mdmResult.Data : null;

                // Loop order
                foreach (var order in listInput)
                {
                    _listInvTransactionByOneShopId = new List<INV_InventoryTransaction>();
                    _osOrderId = order.Id;
                    _oneShopId = order.ExternalOrdNbr;
                    _stockImportAll = true;
                    _importBudgetStatus = true;

                    #region Handle Customer
                    var resCusInfo = _clientService.CommonRequest<ResultModelWithObject<ExGetInfoCusAndShioptoByOutletCodeModel>>(
                        CommonData.SystemUrlCode.ODCustomerAPI,
                        $"CustomerInfomation/ExGetInfoCusAndShiptoByOutletCode/{order.CustomerId}",
                        Method.GET,
                        token,
                        null,
                        true
                       );

                    if (resCusInfo == null)
                    {
                        return new BaseResultModel
                        {
                            IsSuccess = false,
                            Code = 404,
                            Message = "API CustomerInfomation/ExGetInfoCusAndShiptoByOutletCode is not working"
                        };
                    }

                    if (resCusInfo != null && !resCusInfo.IsSuccess)
                    {
                        // Tạo điểm bán
                        // Map data
                        ExCreateCustomer reqData = _mapper.Map<ExCreateCustomer>(order);
                        reqData.DistributorName = _distributorInfo.DistributorName;
                        var resCusInfoCreate = _clientService.CommonRequest<ResultModelWithObject<ExGetInfoCusAndShioptoByOutletCodeModel>>(
                            CommonData.SystemUrlCode.ODCustomerAPI,
                            $"OneShopLinkRequest/ExCreateCustomer",
                            Method.POST,
                            token,
                            reqData,
                            true
                           );

                        if (resCusInfoCreate == null)
                        {
                            return new BaseResultModel
                            {
                                IsSuccess = false,
                                Code = 404,
                                Message = "API OneShopLinkRequest/ExCreateCustomer is not working"
                            };
                        }

                        if (resCusInfoCreate != null && (!resCusInfoCreate.IsSuccess || resCusInfoCreate.Data == null))
                        {
                            return new BaseResultModel
                            {
                                IsSuccess = false,
                                Code = 400,
                                Message = resCusInfoCreate.Message
                            };
                        }

                        _customerInfoCurrent = resCusInfoCreate.Data;
                    }
                    else
                    {
                        _customerInfoCurrent = resCusInfo.Data;
                    }
                    #endregion

                    // Generate RefNumber
                    var prefix = StringsHelper.GetPrefixYYM();
                    var orderRefNumberIndb = await _orderInformationsRepository
                        .GetAllQueryable(null, null, null, _schemaName)
                        .Where(x => x.OrderRefNumber.Contains(prefix))
                        .AsNoTracking().Select(x => x.OrderRefNumber).OrderByDescending(x => x)
                        .FirstOrDefaultAsync();

                    var generatedNumber = StringsHelper.GennerateCodeWithYearMonthFormat(prefix, orderRefNumberIndb != null ? orderRefNumberIndb : null);

                    bool checkExisted = false;
                    do
                    {
                        var settingInDb = await _settingRepository.GetAllQueryable(null, null, null, _schemaName).FirstOrDefaultAsync();
                        if (settingInDb != null && settingInDb.OrderRefNumber == generatedNumber)
                        {
                            checkExisted = false;
                            generatedNumber = string.Format("{0}{1:00000}", prefix, generatedNumber != null ? generatedNumber.Substring(3).TryParseInt() + 1 : 0);
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

                    var orderNew = new SaleOrderModel();
                    var orderItemNews = new List<SO_OrderItems>();
                    order.ImportStatus = IMPORT_STATUS.SUCCESS;

                    // Get all list transaction of OneShopId
                    _listInvTransactionByOneShopId = await _inventoryService.GetTransactionsByOneShopID(_oneShopId);
                    BaseResultModel checkBookedFromInvTransacion = await CheckInvTransactionNegative(_listInvTransactionByOneShopId);
                    if (!checkBookedFromInvTransacion.IsSuccess) return checkBookedFromInvTransacion;
                    // Loop item of order
                    foreach (var item in order.OrderItems)
                    {
                        _stockImport = true;
                        if (item.OriginalOrderQty == 0) continue;
                        if (item.OriginalOrderBaseQty == 0) continue;

                        #region Handle flow budget

                        if (!string.IsNullOrWhiteSpace(item.BudgetCode))
                        {
                            // Cờ check status budget import
                            bool _budgetImport = true;

                            int _budgetBook = item.BudgetBook.HasValue ? item.BudgetBook.Value : 0;
                            int _budgetBooked = item.BudgetBooked.HasValue ? item.BudgetBooked.Value : 0;

                            // Calculate budget need book
                            int _budgetNeedBook = _budgetBook - _budgetBooked;

                            if (_budgetNeedBook > 0)
                            {
                                // Book budget
                                var budgetRequest = new BudgetRequestModel();
                                budgetRequest.budgetCode = item.BudgetCode;
                                budgetRequest.budgetType = null;
                                budgetRequest.customerCode = order.CustomerId;
                                budgetRequest.customerShipTo = null;
                                budgetRequest.saleOrg = _distributorInfo.SalesOrgId;
                                // budgetRequest.budgetAllocationLevel = "DSA"; // đang hardcode temp theo yêu cầu PO NAM
                                budgetRequest.budgetAllocationLevel = null;
                                budgetRequest.budgetBook = _budgetNeedBook;
                                budgetRequest.promotionCode = item.PromotionCode;
                                budgetRequest.promotionLevel = item.PromotionLevelCode;
                                budgetRequest.routeZoneCode = null;
                                budgetRequest.dsaCode = _distributorInfo.DSACode;
                                budgetRequest.salesOrgCode = _distributorInfo.SalesOrgId;
                                budgetRequest.referalCode = null;
                                budgetRequest.distributorCode = _distributorCode;

                                var budgetBookResult = _clientService.CommonRequest<ResultModelWithObject<BudgetResponseModel>>(
                                    CommonData.SystemUrlCode.ODTpAPI,
                                    $"external_checkbudget/checkbudget",
                                    Method.POST,
                                    token,
                                    budgetRequest
                                   ).Data;

                                if (budgetBookResult != null && budgetBookResult.status)
                                {
                                    if (_budgetNeedBook > 0)
                                    {
                                        // Không đủ
                                        if (budgetBookResult.budgetBooked < _budgetNeedBook)
                                        {
                                            // Handle budget book over
                                            if (item.BudgetBookOver.HasValue && item.BudgetBookOver.Value && !string.IsNullOrWhiteSpace(item.BudgetBookOption))
                                            {
                                                // FP/P budgetImport = true
                                                if (item.BudgetBookOption.ToLower() == BUDGET_BOOK_OPTION.FP.ToLower() ||
                                                    item.BudgetBookOption.ToLower() == BUDGET_BOOK_OPTION.P.ToLower())
                                                {
                                                    _budgetImport = true;
                                                }
                                                else // budgetImport = false
                                                {
                                                    _budgetImport = false;
                                                    _importBudgetStatus = false;
                                                }
                                            }
                                            else // budgetImport = false
                                            {
                                                _budgetImport = false;
                                                _importBudgetStatus = false;
                                            }
                                        }

                                        // Book số còn lại
                                        item.BudgetBooked = budgetBookResult.budgetBooked;
                                    }
                                    else
                                    {
                                        item.BudgetBooked += budgetBookResult.budgetBooked;
                                    }
                                }
                                else
                                {
                                    item.BudgetBooked = 0;
                                    _budgetImport = false;
                                    _importBudgetStatus = false;
                                }
                            }

                            item.BudgetImport = _budgetImport;
                        }
                        else
                        {
                            item.BudgetImport = null;
                            //_importBudgetStatus = null;
                        }
                        #endregion
                        // Handle Book inventory
                        item.StockCheckStatus = null;

                        if (item.BudgetImport == null || item.BudgetImport.HasValue && item.BudgetImport.Value)
                        {
                            _listSOOrderItem = new List<SO_OrderItems>();

                            // Handle book inventory
                            var resultHandleBookInventory = await HandleBookInventory(item, order.DistributorCode, _warehouseCode, token);
                            if (!resultHandleBookInventory.IsSuccess)
                                return resultHandleBookInventory;

                            if (item.OriginalOrderQtyBooked < 1)
                            {
                                _stockImport = false;
                                _stockImportAll = false;
                            };

                            // Cập nhật cờ stock import
                            item.StockCheckStatus = _stockImport;

                            if (item.StockCheckStatus.Value)
                            {
                                if (_listSOOrderItem.Count > 0)
                                {

                                    foreach (var itemSkuNew in _listSOOrderItem)
                                    {
                                        itemSkuNew.OrderRefNumber = generatedNumber;
                                    }

                                    orderItemNews.AddRange(_listSOOrderItem);
                                }
                            }
                        }

                        // Update transaction
                        _osOrderItemRepository.UpdateUnSaved(item, _schemaName);
                    } // End Order

                    if (!_importBudgetStatus && !_stockImportAll)
                    {
                        order.ImportStatus = IMPORT_STATUS.FAILED;
                        order.Status = listODMappingStatus.Where(x => x.SaleOrderStatus == SO_SaleOrderStatusConst.OUTOFSTOCKBUDGET).Select(d => d.OneShopOrderStatus).FirstOrDefault();
                        order.SOStatus = SO_SaleOrderStatusConst.OUTOFSTOCKBUDGET;
                        _osStatusCurrent = order.Status;
                    }
                    else if (!_importBudgetStatus)
                    {
                        order.ImportStatus = IMPORT_STATUS.FAILED;
                        order.Status = listODMappingStatus.Where(x => x.SaleOrderStatus == SO_SaleOrderStatusConst.OUTOFBUDGET).Select(d => d.OneShopOrderStatus).FirstOrDefault();
                        order.SOStatus = SO_SaleOrderStatusConst.OUTOFBUDGET;
                        _osStatusCurrent = order.Status;
                    }
                    else if (!_stockImportAll)
                    {
                        order.ImportStatus = IMPORT_STATUS.FAILED;
                        order.Status = listODMappingStatus.Where(x => x.SaleOrderStatus == SO_SaleOrderStatusConst.OUTOFSTOCK).Select(d => d.OneShopOrderStatus).FirstOrDefault();
                        order.SOStatus = SO_SaleOrderStatusConst.OUTOFSTOCK;
                        _osStatusCurrent = order.Status;
                    }

                    if (order.ImportStatus == IMPORT_STATUS.SUCCESS)
                    {
                        order.Status = listODMappingStatus.Where(x => x.SaleOrderStatus == SO_SaleOrderStatusConst.IMPORTSUCCESSFULLY).Select(d => d.OneShopOrderStatus).FirstOrDefault();
                        order.SOStatus = SO_SaleOrderStatusConst.IMPORTSUCCESSFULLY;

                        // Gán mã đơn hàng cho OneShop
                        order.OrderRefNumber = generatedNumber;

                        // Mapping order
                        orderNew = await MappingSOSaleOrder(order, username);
                        orderNew.OrderRefNumber = generatedNumber;

                        // Gán status not mapped
                        orderNew.SOStatus = order.SOStatus;
                        orderNew.OSStatus = order.Status;

                        _osStatusCurrent = listODMappingStatus.Where(x => x.SaleOrderStatus == orderNew.Status).Select(d => d.OneShopOrderStatus).FirstOrDefault();

                        // handle data KIT
                        if (order.OrderItems.FirstOrDefault(x => x.IsKit.HasValue && x.IsKit.Value) != null)
                        {
                            var groupKits = order.OrderItems.GroupBy(x => new { x.IsKit.Value, x.KitId }).Select(x => x.First()).Where(x => x.KitId != null).ToList();
                            foreach (var kitNew in groupKits)
                            {
                                var _kitInDb = await _kitRepository.GetAllQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.ItemKitId == kitNew.KitId);
                                if (_kitInDb != null)
                                {
                                    var itemKit = new SO_OrderItems();
                                    itemKit.Id = Guid.NewGuid();
                                    itemKit.OrderRefNumber = generatedNumber;
                                    itemKit.InventoryID = kitNew.KitId;
                                    itemKit.KitId = _kitInDb.Id;
                                    itemKit.IsKit = true;
                                    itemKit.LocationID = _locationDefaultCurrent.Code.ToString();
                                    itemKit.ItemId = Guid.Empty;
                                    itemKit.ItemCode = null;
                                    itemKit.ItemDescription = _kitInDb.Description;
                                    itemKit.UOM = kitNew.KitUomId;
                                    itemKit.UnitRate = 1;
                                    itemKit.OriginalOrderQuantities = kitNew.KitQuantity.HasValue ? kitNew.KitQuantity.Value : 0;
                                    itemKit.OriginalOrderBaseQuantities = kitNew.KitQuantity.HasValue ? kitNew.KitQuantity.Value : 0;
                                    itemKit.OrderQuantities = itemKit.OriginalOrderQuantities;
                                    itemKit.OrderBaseQuantities = itemKit.OriginalOrderBaseQuantities;
                                    itemKit.VatId = _kitInDb.Vat;
                                    itemKit.BaseUnit = _kitInDb.BaseUnit;
                                    itemKit.KitKey = $"Kit_{_kitInDb.ItemKitId}_{itemKit.Id}";
                                    itemKit.PurchaseUnit = _kitInDb.PurchaseUnit;
                                    itemKit.SalesUnit = _kitInDb.SalesUnit;
                                    itemKit.Orig_Ord_Line_Amt = kitNew.KitAmount.HasValue ? (decimal)kitNew.KitAmount.Value : 0;
                                    itemKit.UnitPrice = kitNew.UnitPrice.HasValue ? (decimal)kitNew.UnitPrice.Value : 0;
                                    itemKit.Ord_Line_Amt = itemKit.Orig_Ord_Line_Amt;

                                    orderItemNews.Add(itemKit);
                                }
                            }
                        }

                        // Gán list item cho Order
                        orderNew.OrderItems = orderItemNews;

                        var resultCreateSOOrder = await _salesOrderService.InsertOrderFromOneShop(orderNew, _listInvTransactionByOneShopId, token, listODMappingStatus);
                        if (!resultCreateSOOrder.IsSuccess) return resultCreateSOOrder;

                        // Update order ref number item ffa

                        foreach (var itemOsUpdate in order.OrderItems)
                        {
                            itemOsUpdate.OrderRefNumber = generatedNumber;
                            _osOrderItemRepository.UpdateUnSaved(itemOsUpdate, _schemaName);
                        }
                    }
                    else // Save status history
                    {
                        var statusMappingCurrent = listODMappingStatus.Where(x => x.SaleOrderStatus == order.SOStatus && x.OneShopOrderStatus == order.Status).FirstOrDefault();
                        // Save status
                        OsorderStatusHistory hisStatusNew = new();
                        hisStatusNew.OrderRefNumber = null;
                        hisStatusNew.ExternalOrdNbr = order.ExternalOrdNbr;
                        hisStatusNew.OrderDate = order.OrderDate;
                        hisStatusNew.DistributorCode = _distributorInfo.DistributorCode;
                        hisStatusNew.Sostatus = order.SOStatus;
                        // Handle status name for saleorder
                        hisStatusNew.SOStatusName = statusMappingCurrent?.SaleOrderStatusName;
                        hisStatusNew.OneShopStatus = order.Status;
                        // Handle status name for oneshop
                        hisStatusNew.OneShopStatusName = statusMappingCurrent?.OneShopOrderStatusName;

                        hisStatusNew.CreatedBy = _createdBy;
                        hisStatusNew.OutletCode = order.CustomerId;
                        
                        BaseResultModel resultSaveStatusHis = await _orderStatusHisService.SaveStatusHistory(hisStatusNew, false);
                        if (!resultSaveStatusHis.IsSuccess) return resultSaveStatusHis;
                    }

                    order.UpdatedDate = DateTime.Now;

                    _osOrderRepository.UpdateUnSaved(order, _schemaName);
                    _osOrderRepository.Save(_schemaName);

                    Serilog.Log.Information($"############ Chuẩn bị vào SendNotification : {order.Status} - {order.SOStatus}");
                    // Send notification
                    OSNotificationModel reqNoti = new();
                    reqNoti.External_OrdNBR = order.ExternalOrdNbr;
                    reqNoti.OrderRefNumber = order.OrderRefNumber;
                    reqNoti.OSStatus = order.Status;
                    reqNoti.SOStatus = order.SOStatus;
                    reqNoti.DistributorCode = _distributorInfo.DistributorCode;
                    reqNoti.DistributorName = _distributorInfo.DistributorName;
                    reqNoti.OutletCode = order.CustomerId;
                    reqNoti.Purpose = OSNotificationPurpose.GetPurpose(order.SOStatus);

                    await _osNotifiService.SendNotification(reqNoti, token);
                }

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success"
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

        public async Task<BaseResultModel> HandleCancelOSOrder(List<OsOrderModel> listInput, string token, string username, bool isFromOs = true)
        {
            try
            {
                Stopwatch stopWatch1 = new Stopwatch();
                stopWatch1.Start();
                Serilog.Log.Information($"############ Start Get list OD Mapping Status + Location + VAT");
                

                List<ODMappingOrderStatus> listODMappingStatus = await _odMappingOrderStatusRepo.GetAllQueryable().ToListAsync();
                _createdBy = username;
                #region Handle location stock
                ResultModelWithObject<List<PrincipalWarehouseLocation>> principalLocationList = await _inventoryService.GetListPrincipalWarehouseLocation();
                if (principalLocationList.IsSuccess &&
                    principalLocationList.Data != null &&
                    principalLocationList.Data.Count > 0)
                {
                    _locationDefaultCurrent = principalLocationList.Data.FirstOrDefault(x => x.IsDefault);
                }
                else
                {
                    return new BaseResultModel()
                    {
                        Code = principalLocationList.Code,
                        Message = principalLocationList.Message,
                        IsSuccess = false
                    };
                }
                #endregion

                // Get list VAT
                _listVats = await _vatRepository.GetAllQueryable().AsNoTracking().ToListAsync();

                stopWatch1.Stop();
                Serilog.Log.Information($"############ End Get list OD Mapping Status + Location + VAT: {stopWatch1.ElapsedMilliseconds}");

                Stopwatch stopWatch2 = new Stopwatch();
                stopWatch2.Start();
                Serilog.Log.Information($"############ Start Handle SalePeriod");
                
                #region Handle SalePeriod
                BaseResultModel resGetPeriod = await _salesOrderService.GetPeriodID(listInput.First().OrderDate.Value, token);

                if (!resGetPeriod.IsSuccess)
                {
                    return resGetPeriod;
                }

                _periodID = resGetPeriod?.Data?.ToString();
                #endregion

                stopWatch2.Stop();
                Serilog.Log.Information($"############ End Handle SalePeriod: {stopWatch2.ElapsedMilliseconds}");


                Stopwatch stopWatch3 = new Stopwatch();
                stopWatch3.Start();
                Serilog.Log.Information($"############ Start Handle Distributor");

                
                #region Handle Distributor
                string distributorCode = listInput.First().DistributorCode;

                // Handle distributor shipto
                var resGetShiptoActive = _clientService.CommonRequest<ResultModelWithObject<List<DisShipto>>>(
                    CommonData.SystemUrlCode.ODDistributorAPI,
                    $"DistributorShipto/ExGetListShiptoActiveByDistributorCode/{_distributorCode}",
                    Method.GET,
                    $"Rdos {token.Split(" ").Last()}",
                    null);

                if (!resGetShiptoActive.IsSuccess || resGetShiptoActive.Data.Count == 0)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 404,
                        Message = "Cannot found warehouse"
                    };
                }

                _warehouseCode = resGetShiptoActive.Data.First().ShiptoCode;

                var resDisInfo = _clientService.CommonRequest<ResultModelWithObject<DistributorInfoModel>>(
                    CommonData.SystemUrlCode.SalesOrgAPI,
                    $"DistributorSellingArea/GetInformationDistributor/{distributorCode}",
                    Method.GET,
                    token,
                    null
                   );

                if (resDisInfo == null || resDisInfo.Data == null)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 404,
                        Message = "Distributor not found in saleOrg"
                    };
                }

                _distributorInfo = resDisInfo.Data;
                _distributorInfo.DistributorName = resGetShiptoActive.Data.First().Name;

                // MDM SaleOrg
                var mdmResult = _clientService.CommonRequest<ResultModelWithObject<List<MdmModel>>>(
                    CommonData.SystemUrlCode.SalesOrgAPI,
                    $"DistributorSellingArea/GetListEmployeeManager/{_distributorInfo.DSACode}",
                    RestSharp.Method.GET,
                    token,
                    null
                   );

                _mdmDistributor = mdmResult.IsSuccess ? mdmResult.Data : null;
                #endregion
                stopWatch3.Stop();
                Serilog.Log.Information($"############ End Handle Distributor: {stopWatch3.ElapsedMilliseconds}");

                Stopwatch stopWatch4 = new Stopwatch();
                stopWatch4.Start();
                Serilog.Log.Information($"############ Start Handle order");
                
                // Loop order
                foreach (var order in listInput)
                {
                    _listInvTransactionByOneShopId = new List<INV_InventoryTransaction>();
                    _osOrderId = order.Id;
                    _oneShopId = order.ExternalOrdNbr;
                    _stockImportAll = true;
                    _importBudgetStatus = true;

                    Stopwatch stopWatch5 = new Stopwatch();
                    stopWatch5.Start();
                    Serilog.Log.Information($"############ Start Handle customer");
                    
                    #region Handle Customer
                    var resCusInfo = _clientService.CommonRequest<ResultModelWithObject<ExGetInfoCusAndShioptoByOutletCodeModel>>(
                        CommonData.SystemUrlCode.ODCustomerAPI,
                        $"CustomerInfomation/ExGetInfoCusAndShiptoByOutletCode/{order.CustomerId}",
                        Method.GET,
                        token,
                        null,
                        true
                       );

                    if (resCusInfo == null)
                    {
                        return new BaseResultModel
                        {
                            IsSuccess = false,
                            Code = 404,
                            Message = "API CustomerInfomation/ExGetInfoCusAndShiptoByOutletCode is not working"
                        };
                    }

                    if (resCusInfo != null && !resCusInfo.IsSuccess)
                    {
                        // Tạo điểm bán
                        // Map data
                        ExCreateCustomer reqData = _mapper.Map<ExCreateCustomer>(order);
                        reqData.DistributorName = _distributorInfo.DistributorName;
                        var resCusInfoCreate = _clientService.CommonRequest<ResultModelWithObject<ExGetInfoCusAndShioptoByOutletCodeModel>>(
                            CommonData.SystemUrlCode.ODCustomerAPI,
                            $"OneShopLinkRequest/ExCreateCustomer",
                            Method.POST,
                            token,
                            reqData,
                            true
                           );

                        if (resCusInfoCreate == null)
                        {
                            return new BaseResultModel
                            {
                                IsSuccess = false,
                                Code = 404,
                                Message = "API OneShopLinkRequest/ExCreateCustomer is not working"
                            };
                        }

                        if (resCusInfoCreate != null && (!resCusInfoCreate.IsSuccess || resCusInfoCreate.Data == null))
                        {
                            return new BaseResultModel
                            {
                                IsSuccess = false,
                                Code = 400,
                                Message = resCusInfoCreate.Message
                            };
                        }

                        _customerInfoCurrent = resCusInfoCreate.Data;
                    }
                    else
                    {
                        _customerInfoCurrent = resCusInfo.Data;
                    }
                    #endregion

                    _customerInfoCurrent = resCusInfo.Data;
                    stopWatch5.Stop();
                    Serilog.Log.Information($"############ End Handle customer: {stopWatch5.ElapsedMilliseconds}");

                    #region Handle Generate RefNumber
                    var prefix = StringsHelper.GetPrefixYYM();
                    var orderRefNumberIndb = await _orderInformationsRepository
                        .GetAllQueryable(null, null, null, _schemaName)
                        .Where(x => x.OrderRefNumber.Contains(prefix))
                        .AsNoTracking().Select(x => x.OrderRefNumber).OrderByDescending(x => x)
                        .FirstOrDefaultAsync();

                    var generatedNumber = StringsHelper.GennerateCodeWithYearMonthFormat(prefix, orderRefNumberIndb != null ? orderRefNumberIndb : null);

                    bool checkExisted = false;
                    do
                    {
                        var settingInDb = await _settingRepository.GetAllQueryable(null, null, null, _schemaName).FirstOrDefaultAsync();
                        if (settingInDb != null && settingInDb.OrderRefNumber == generatedNumber)
                        {
                            checkExisted = false;
                            generatedNumber = string.Format("{0}{1:00000}", prefix, generatedNumber != null ? generatedNumber.Substring(3).TryParseInt() + 1 : 0);
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
                    #endregion

                    var orderNew = new SaleOrderModel();
                    var orderItemNews = new List<SO_OrderItems>();
                    order.ImportStatus = IMPORT_STATUS.SUCCESS;

                    // Get all list transaction of OneShopId
                    _listInvTransactionByOneShopId = await _inventoryService.GetTransactionsByOneShopID(_oneShopId);

                    // Loop item of order
                    foreach (var item in order.OrderItems)
                    {
                        _stockImport = true;
                        if (item.OriginalOrderQty == 0) continue;
                        if (item.OriginalOrderBaseQty == 0) continue;

                        #region Handle flow budget

                        if (!string.IsNullOrWhiteSpace(item.BudgetCode))
                        {
                            // Cờ check status budget import
                            bool _budgetImport = true;

                            int _budgetBooked = item.BudgetBooked.HasValue ? item.BudgetBooked.Value : 0;

                            if (_budgetBooked > 0)
                            {
                                // Trả số đã book nếu có Book budget
                                var budgetRequest = new BudgetRequestModel();
                                budgetRequest.budgetCode = item.BudgetCode;
                                budgetRequest.budgetType = null;
                                budgetRequest.customerCode = order.CustomerId;
                                budgetRequest.customerShipTo = null;
                                budgetRequest.saleOrg = _distributorInfo.SalesOrgId;
                                // budgetRequest.budgetAllocationLevel = "DSA"; // đang hardcode temp theo yêu cầu PO NAM
                                budgetRequest.budgetAllocationLevel = null; 
                                budgetRequest.budgetBook = -(_budgetBooked);
                                budgetRequest.promotionCode = item.PromotionCode;
                                budgetRequest.promotionLevel = item.PromotionLevelCode;
                                budgetRequest.routeZoneCode = null;
                                budgetRequest.dsaCode = _distributorInfo.DSACode;
                                budgetRequest.salesOrgCode = _distributorInfo.SalesOrgId;
                                budgetRequest.referalCode = null;
                                budgetRequest.distributorCode = _distributorCode;

                                var budgetBookResult = _clientService.CommonRequest<ResultModelWithObject<BudgetResponseModel>>(
                                    CommonData.SystemUrlCode.ODTpAPI,
                                    $"external_checkbudget/checkbudget",
                                    Method.POST,
                                    token,
                                    budgetRequest
                                   ).Data;

                                if (budgetBookResult != null && budgetBookResult.status)
                                {
                                    // Book số còn lại
                                    item.BudgetBooked -= _budgetBooked;
                                }
                            }
                            item.BudgetImport = _budgetImport;
                        }
                        else
                        {
                            item.BudgetImport = null;
                        }
                        #endregion

                        // Handle Book inventory
                        item.StockCheckStatus = null;

                        Stopwatch stopWatch6 = new Stopwatch();
                        stopWatch6.Start();
                        Serilog.Log.Information($"############ Start Handle Item ");
                        
                        if (item.BudgetImport == null || item.BudgetImport.HasValue && item.BudgetImport.Value)
                        {
                            _listSOOrderItem = new List<SO_OrderItems>();

                            // Handle book inventory
                            var resultHandleBookInventory = await HandleCancelInventory(item, order.DistributorCode, _warehouseCode, token);
                            if (!resultHandleBookInventory.IsSuccess)
                                return resultHandleBookInventory;

                            // Cập nhật cờ stock import
                            item.StockCheckStatus = _stockImport;

                            if (item.StockCheckStatus.Value)
                            {
                                if (_listSOOrderItem.Count > 0)
                                {

                                    foreach (var itemSkuNew in _listSOOrderItem)
                                    {
                                        itemSkuNew.OrderRefNumber = generatedNumber;
                                    }

                                    orderItemNews.AddRange(_listSOOrderItem);
                                }
                            }
                        }

                        // Update transaction
                        _osOrderItemRepository.UpdateUnSaved(item, _schemaName);

                        stopWatch6.Stop();
                        Serilog.Log.Information($"############ End Start Handle Item: {stopWatch6.ElapsedMilliseconds}");
                    } // End Order

                    Stopwatch stopWatch7 = new Stopwatch();
                    stopWatch7.Start();
                    Serilog.Log.Information($"############ Start Create SO");
                    

                    order.ImportStatus = IMPORT_STATUS.SUCCESS;
                    order.SOStatus = SO_SaleOrderStatusConst.CANCEL;
                    var mappingStatusInDb = await _orderStatusHisService.HandleOSMappingStatus(order.SOStatus, isFromOs);
                    order.Status = mappingStatusInDb?.OneShopOrderStatus;


                    // Map order header
                    // Gán mã đơn hàng cho OneShop
                    order.OrderRefNumber = generatedNumber;

                    orderNew = await MappingSOSaleOrder(order, username);
                    orderNew.OrderRefNumber = generatedNumber;
                    orderNew.Status = SO_SaleOrderStatusConst.CANCEL;
                    orderNew.OSStatus = order.Status;
                    orderNew.SOStatus = order.SOStatus;
                    // handle data KIT
                    if (order.OrderItems.FirstOrDefault(x => x.IsKit.HasValue && x.IsKit.Value) != null)
                    {
                        var groupKits = order.OrderItems.GroupBy(x => new { x.IsKit.Value, x.KitId }).Select(x => x.First()).Where(x => x.KitId != null).ToList();
                        foreach (var kitNew in groupKits)
                        {
                            var _kitInDb = await _kitRepository.GetAllQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.ItemKitId == kitNew.KitId);
                            if (_kitInDb != null)
                            {
                                var itemKit = new SO_OrderItems();
                                itemKit.Id = Guid.NewGuid();
                                itemKit.OrderRefNumber = generatedNumber;
                                itemKit.InventoryID = kitNew.KitId;
                                itemKit.KitId = _kitInDb.Id;
                                itemKit.IsKit = true;
                                itemKit.LocationID = _locationDefaultCurrent.Code.ToString();
                                itemKit.ItemId = Guid.Empty;
                                itemKit.ItemCode = null;
                                itemKit.ItemDescription = _kitInDb.Description;
                                itemKit.UOM = kitNew.KitUomId;
                                itemKit.UnitRate = 1;
                                itemKit.OriginalOrderQuantities = kitNew.KitQuantity.HasValue ? kitNew.KitQuantity.Value : 0;
                                itemKit.OriginalOrderBaseQuantities = kitNew.KitQuantity.HasValue ? kitNew.KitQuantity.Value : 0;
                                itemKit.OrderQuantities = itemKit.OriginalOrderQuantities;
                                itemKit.OrderBaseQuantities = itemKit.OriginalOrderBaseQuantities;
                                itemKit.VatId = _kitInDb.Vat;
                                itemKit.BaseUnit = _kitInDb.BaseUnit;
                                itemKit.KitKey = $"Kit_{_kitInDb.ItemKitId}_{itemKit.Id}";
                                itemKit.PurchaseUnit = _kitInDb.PurchaseUnit;
                                itemKit.SalesUnit = _kitInDb.SalesUnit;
                                itemKit.Orig_Ord_Line_Amt = kitNew.KitAmount.HasValue ? (decimal)kitNew.KitAmount.Value : 0;
                                itemKit.UnitPrice = kitNew.UnitPrice.HasValue ? (decimal)kitNew.UnitPrice.Value : 0;
                                itemKit.Ord_Line_Amt = itemKit.Orig_Ord_Line_Amt;

                                orderItemNews.Add(itemKit);
                            }
                        }
                    }

                    // Gán list item cho Order
                    orderNew.OrderItems = orderItemNews;

                    var resultCreateSOOrder = await _salesOrderService.InsertOrderFromOneShop(orderNew, _listInvTransactionByOneShopId, token, listODMappingStatus);
                    if (!resultCreateSOOrder.IsSuccess) return resultCreateSOOrder;

                    // Update OrderRefNumber
                    foreach (var itemOsUpdate in order.OrderItems)
                    {
                        itemOsUpdate.OrderRefNumber = generatedNumber;
                        _osOrderItemRepository.UpdateUnSaved(itemOsUpdate, _schemaName);
                    }

                    order.UpdatedDate = DateTime.Now;

                    _osOrderRepository.UpdateUnSaved(order, _schemaName);

                    // Save all data
                    _osOrderRepository.Save(_schemaName);

                    stopWatch7.Stop();
                    Serilog.Log.Information($"############ End  Create SO: {stopWatch7.ElapsedMilliseconds}");
                    Serilog.Log.Information($"############ Chuẩn bị vào SendNotification : {order.Status} - {order.SOStatus}");
                    if (!isFromOs)
                    {
                        // Send notification
                        OSNotificationModel reqNoti = new();
                        reqNoti.External_OrdNBR = order.ExternalOrdNbr;
                        reqNoti.OrderRefNumber = order.OrderRefNumber;
                        reqNoti.OSStatus = order.Status;
                        reqNoti.SOStatus = order.SOStatus;
                        reqNoti.DistributorCode = _distributorInfo.DistributorCode;
                        reqNoti.DistributorName = _distributorInfo.DistributorName;
                        reqNoti.OutletCode = order.CustomerId;
                        reqNoti.Purpose = OSNotificationPurpose.GetPurpose(order.SOStatus);

                        await _osNotifiService.SendNotification(reqNoti, token, isFromOs);
                    }
                }

                stopWatch4.Stop();
                Serilog.Log.Information($"############ End Handle order: {stopWatch4.ElapsedMilliseconds}");

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success"
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

        public async Task<SaleOrderModel> MappingSOSaleOrder(OsOrderModel osModelReq, string userName)
        {
            SaleOrderModel soModel = new SaleOrderModel();
            soModel.Id = Guid.NewGuid();
            soModel.OrderRefNumber = osModelReq.OrderRefNumber;
            soModel.NotInSubRoute = false;
            soModel.IsDirect = osModelReq.IsDirect.HasValue ? osModelReq.IsDirect.Value : false;
            soModel.OrderType = SO_SaleOrderTypeConst.SalesOrder;
            soModel.WareHouseID = _warehouseCode;
            soModel.PrincipalID = osModelReq.PrincipalId;
            soModel.DistributorCode = _distributorInfo.DistributorCode;
            soModel.isReturn = false;
            soModel.Status = SO_SaleOrderStatusConst.OPEN;
            soModel.IsPrintedDeliveryNote = false;
            soModel.SalesOrgID = _distributorInfo.SalesOrgId;
            soModel.TerritoryStrID = _distributorInfo.TerritoryStructureCode;
            soModel.TerritoryValueKey = _distributorInfo.TerritoryValueKey;
            soModel.DSAID = _distributorInfo.DSACode;
            soModel.MenuType = MenuTypeConst.SO_Menu02;
            soModel.ExpectShippedDate = osModelReq.ExpectShippedDate;
            soModel.OrderDate = osModelReq.OrderDate.Value;
            soModel.External_OrdNBR = osModelReq.ExternalOrdNbr;
            soModel.Owner_ID = _distributorInfo.DistributorCode;
            soModel.Source = SO_SOURCE_CONST.ONESHOP;
            soModel.OrderDescription = osModelReq.OrderDescription;
            soModel.Note = osModelReq.DeliveryAddressNote;
            soModel.DiscountID = osModelReq.DiscountId;
            soModel.OSOutletCode = osModelReq.CustomerId;
            soModel.DistributorName = _distributorInfo.DistributorName;
            soModel.SalesRepID = _distributorInfo.DistributorCode;

            // Handle MDM of Distributor
            if (_mdmDistributor != null && _mdmDistributor.Count > 0)
            {
                foreach (var item in _mdmDistributor)
                {
                    if (item.IsDsa)
                    {
                        soModel.DSA_Manager_ID = item.EmployeeCode;
                    }
                    else
                    {
                        switch (item.TerritoryLevel)
                        {
                            case TerritorySettingConst.Branch:
                                {
                                    soModel.Branch_Manager_ID = item.EmployeeCode;
                                    soModel.BranchId = item.TerritoryValue.Split("-").Last().Trim();
                                    break;
                                }
                            case TerritorySettingConst.Region:
                                {
                                    soModel.Region_Manager_ID = item.EmployeeCode;
                                    soModel.RegionId = item.TerritoryValue.Split("-").Last().Trim();
                                    break;
                                }
                            case TerritorySettingConst.SubRegion:
                                {
                                    soModel.Sub_Region_Manager_ID = item.EmployeeCode;
                                    soModel.SubRegionId = item.TerritoryValue.Split("-").Last().Trim();
                                    break;
                                }
                            case TerritorySettingConst.Area:
                                {
                                    soModel.Area_Manager_ID = item.EmployeeCode;
                                    soModel.AreaId = item.TerritoryValue.Split("-").Last().Trim();
                                    break;
                                }
                            case TerritorySettingConst.SubArea:
                                {
                                    soModel.Sub_Area_Manager_ID = item.EmployeeCode;
                                    soModel.SubAreaId = item.TerritoryValue.Split("-").Last().Trim();
                                    break;
                                }
                            case null:
                                {
                                    if (item.Source == "Country")
                                    {
                                        soModel.NSD_ID = item.EmployeeCode;
                                    }
                                    break;
                                }
                            default: break;
                        }
                    }
                }
            }

            // Xử lý customer and customer shipto -- update lated
            soModel.CustomerId = _customerInfoCurrent?.CustomerCode;
            soModel.CustomerName = osModelReq.CustomerName;
            soModel.CustomerAddress = osModelReq.CustomerAddress;
            soModel.CustomerPhone = osModelReq.CustomerPhone;
            soModel.CusAddressCountryId = osModelReq.CusAddressCountryId;
            soModel.CusAddressProvinceId = osModelReq.CusAddressProvinceId;
            soModel.CusAddressDistrictId = osModelReq.CusAddressDistrictId;
            soModel.CusAddressWardId = osModelReq.CusAddressWardId;
            soModel.CustomerShiptoID = _customerInfoCurrent?.CustomerShiptoCode;
            soModel.CustomerShiptoName = _customerInfoCurrent?.CustomerShiptoName;
            soModel.Shipto_Attribute1 = _customerInfoCurrent?.CustomerShiptoAttribute1;
            soModel.Shipto_Attribute10 = _customerInfoCurrent?.CustomerShiptoAttribute2;
            soModel.Shipto_Attribute2 = _customerInfoCurrent?.CustomerShiptoAttribute3;
            soModel.Shipto_Attribute3 = _customerInfoCurrent?.CustomerShiptoAttribute4;
            soModel.Shipto_Attribute4 = _customerInfoCurrent?.CustomerShiptoAttribute5;
            soModel.Shipto_Attribute5 = _customerInfoCurrent?.CustomerShiptoAttribute6;
            soModel.Shipto_Attribute6 = _customerInfoCurrent?.CustomerShiptoAttribute7;
            soModel.Shipto_Attribute7 = _customerInfoCurrent?.CustomerShiptoAttribute8;
            soModel.Shipto_Attribute8 = _customerInfoCurrent?.CustomerShiptoAttribute9;
            soModel.Shipto_Attribute9 = _customerInfoCurrent?.CustomerShiptoAttribute10;

            // Total đơn hàng
            soModel.Orig_Ord_SKUs = osModelReq.OrigOrdSkus.HasValue ? osModelReq.OrigOrdSkus.Value : 0;
            soModel.Ord_SKUs = osModelReq.OrigOrdSkus.HasValue ? osModelReq.OrigOrdSkus.Value : 0;
            soModel.Orig_Ord_Qty = osModelReq.OrigOrdQty.HasValue ? osModelReq.OrigOrdQty.Value : 0;
            soModel.Ord_Qty = osModelReq.OrigOrdQty.HasValue ? osModelReq.OrigOrdQty.Value : 0;
            soModel.Orig_Promotion_Qty = osModelReq.OrigPromotionQty.HasValue ? osModelReq.OrigPromotionQty.Value : 0;
            soModel.Promotion_Qty = osModelReq.OrigPromotionQty.HasValue ? osModelReq.OrigPromotionQty.Value : 0;
            soModel.Orig_Ord_Amt = osModelReq.OrigOrdAmt.HasValue ? (decimal)osModelReq.OrigOrdAmt.Value : 0;
            soModel.Ord_Amt = osModelReq.OrigOrdAmt.HasValue ? (decimal)osModelReq.OrigOrdAmt.Value : 0;
            soModel.Promotion_Amt = osModelReq.PromotionAmt.HasValue ? (decimal)osModelReq.PromotionAmt.Value : 0;
            soModel.Orig_Ord_Disc_Amt = osModelReq.OrigOrdDiscAmt.HasValue ? (decimal)osModelReq.OrigOrdDiscAmt.Value : 0;
            soModel.Ord_Disc_Amt = osModelReq.OrigOrdDiscAmt.HasValue ? (decimal)osModelReq.OrigOrdDiscAmt.Value : 0;
            soModel.Orig_Ordline_Disc_Amt = osModelReq.OrigOrdlineDiscAmt.HasValue ? (decimal)osModelReq.OrigOrdlineDiscAmt.Value : 0;
            soModel.Ordline_Disc_Amt = osModelReq.OrigOrdlineDiscAmt.HasValue ? (decimal)osModelReq.OrigOrdlineDiscAmt.Value : 0;
            soModel.Orig_Ord_Extend_Amt = osModelReq.OrigOrdExtendAmt.HasValue ? (decimal)osModelReq.OrigOrdExtendAmt.Value : 0;
            soModel.Ord_Extend_Amt = osModelReq.OrigOrdExtendAmt.HasValue ? (decimal)osModelReq.OrigOrdExtendAmt.Value : 0;

            soModel.IsDeleted = false;
            soModel.CreatedDate = DateTime.Now;
            soModel.CreatedBy = userName;
            soModel.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
            soModel.OwnerCode = _distributorInfo.DistributorCode;
            soModel.PeriodID = _periodID;
            soModel.PrincipalID = Environment.GetEnvironmentVariable("PRINCIPALCODE");

            return soModel;
        }

        private async Task<SO_OrderItems> MappingSOOrderItems(OsOrderItem osModelReq, int OrderQty, int OrderBaseQty)
        {
            // Get vat id
            Vat vatCurrent = _listVats.FirstOrDefault(x => x.VatId == osModelReq.Vatcode);

            SO_OrderItems soItemModel = new SO_OrderItems();
            soItemModel.Id = Guid.NewGuid();
            soItemModel.InventoryID = _detailSKUCurrent.InventoryItemId;
            soItemModel.KitId = _KitId;
            soItemModel.IsKit = osModelReq.IsKit.HasValue ? osModelReq.IsKit.Value : false;
            soItemModel.LocationID = _locationDefaultCurrent.Code.ToString();
            soItemModel.ItemId = _detailSKUCurrent.Id;
            soItemModel.ItemCode = _detailSKUCurrent.InventoryItemId;
            soItemModel.ItemDescription = _detailSKUCurrent.Description;
            soItemModel.UOM = osModelReq.Uom;
            soItemModel.UOMDesc = osModelReq.Uomdesc;
            soItemModel.UnitRate = (int)osModelReq.UnitRate;
            soItemModel.OriginalOrderQuantities = OrderQty;
            soItemModel.OriginalOrderBaseQuantities = OrderBaseQty;
            soItemModel.OrderQuantities = OrderQty;
            soItemModel.OrderBaseQuantities = OrderBaseQty;
            soItemModel.VatValue = osModelReq.VatValue.HasValue ? (decimal)osModelReq.VatValue : 0;
            soItemModel.VATCode = osModelReq.Vatcode;
            soItemModel.VatId = vatCurrent != null ? vatCurrent.Id : Guid.Empty;
            soItemModel.IsFree = osModelReq.IsFree.HasValue ? osModelReq.IsFree.Value : false;
            soItemModel.PromotionType = osModelReq.PromotionType;
            soItemModel.PromotionDescription = osModelReq.PromotionDescription;
            soItemModel.UnitPrice = (decimal)osModelReq.UnitPrice;
            soItemModel.InventoryAttibute1 = _detailSKUCurrent.AttributeCode1;
            soItemModel.InventoryAttibute2 = _detailSKUCurrent.AttributeCode2;
            soItemModel.InventoryAttibute3 = _detailSKUCurrent.AttributeCode3;
            soItemModel.InventoryAttibute4 = _detailSKUCurrent.AttributeCode4;
            soItemModel.InventoryAttibute5 = _detailSKUCurrent.AttributeCode5;
            soItemModel.InventoryAttibute6 = _detailSKUCurrent.AttributeCode6;
            soItemModel.InventoryAttibute7 = _detailSKUCurrent.AttributeCode7;
            soItemModel.InventoryAttibute8 = _detailSKUCurrent.AttributeCode8;
            soItemModel.InventoryAttibute9 = _detailSKUCurrent.AttributeCode9;
            soItemModel.InventoryAttibute10 = _detailSKUCurrent.AttributeCode10;
            soItemModel.ItemGroupCode = _detailSKUCurrent.ItemGroupCode;
            soItemModel.BaseUnit = _detailSKUCurrent.BaseUnit;
            soItemModel.PurchaseUnit = _detailSKUCurrent.PurchaseUnit;
            soItemModel.SalesUnit = _detailSKUCurrent.SalesUnit;
            soItemModel.BaseUnitCode = _detailSKUCurrent.BaseUOMCode;
            soItemModel.PurchaseUnitCode = _detailSKUCurrent.PurchaseUOMCode;
            soItemModel.SalesUnitCode = _detailSKUCurrent.SalesUOMCode;
            soItemModel.Hierarchy = _detailSKUCurrent.Hierarchy;
            soItemModel.ItemShortName = _detailSKUCurrent.ShortName;

            // handle data Price
            soItemModel.Orig_Ord_Line_Amt = soItemModel.UnitPrice * soItemModel.OrderQuantities;
            soItemModel.Ord_Line_Amt = soItemModel.UnitPrice * soItemModel.OrderQuantities;
            soItemModel.Shipped_Line_Amt = 0;

            if (osModelReq.OrigOrdLineDiscAmt.HasValue && osModelReq.OriginalOrderQty.HasValue)
            {
                soItemModel.Orig_Ord_line_Disc_Amt = (decimal)osModelReq.OrigOrdLineDiscAmt.Value * soItemModel.OrderQuantities / osModelReq.OriginalOrderQty.Value;
            }
            else
            {
                soItemModel.Orig_Ord_line_Disc_Amt = 0;
            }

            soItemModel.Ord_line_Disc_Amt = soItemModel.Orig_Ord_line_Disc_Amt;
            soItemModel.Shipped_line_Disc_Amt = 0;
            soItemModel.Orig_Ord_Line_Extend_Amt = soItemModel.Orig_Ord_Line_Amt - soItemModel.Orig_Ord_line_Disc_Amt;
            soItemModel.Ord_Line_Extend_Amt = soItemModel.Ord_Line_Amt - soItemModel.Ord_line_Disc_Amt;
            soItemModel.Shipped_Line_Extend_Amt = 0;
            soItemModel.DiscountType = osModelReq.DiscountType;
            soItemModel.DisCountAmount = osModelReq.DisCountAmount.HasValue ? (decimal)osModelReq.DisCountAmount : 0;
            soItemModel.DiscountPercented = osModelReq.DiscountPercented.HasValue ? osModelReq.DiscountPercented.Value : 0;
            soItemModel.PromotionCode = osModelReq.PromotionCode;
            soItemModel.RewardDescription = osModelReq.RewardDescription;
            soItemModel.PromotionLevel = osModelReq.PromotionLevelCode;
            soItemModel.VAT = 0;
            if (soItemModel.VatValue != 0)
            {
                soItemModel.VAT = soItemModel.Ord_Line_Amt * ((decimal)soItemModel.VatValue / 100);
            }

            soItemModel.IsDeleted = false;
            soItemModel.CreatedDate = DateTime.Now;
            soItemModel.CreatedBy = _createdBy;
            soItemModel.OwnerCode = _distributorInfo.DistributorCode;
            soItemModel.OwnerType = OwnerTypeConstant.DISTRIBUTOR;

            return soItemModel;
        }

        #endregion
    }
}
