using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sys.Common.Models;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Sys.Common.Helper;
using ODSaleOrder.API.Services.SaleOrder.Interface;
using ODSaleOrder.API.Services.Base;
using ODSaleOrder.API.Infrastructure;
using ODSaleOrder.API.Models;
using static SysAdmin.API.Constants.Constant;
using ODSaleOrder.API.Infrastructure.SOInfrastructure;
using RestSharp.Authenticators;
using RestSharp;
using SysAdmin.Models.StaticValue;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using RDOS.INVAPI.Infratructure;
using DynamicSchema.Helper.Services.Interface;
using DynamicSchema.Helper.Services;
using ODSaleOrder.API.Services.Inventory;
using Microsoft.AspNetCore.Http;
using static System.Runtime.InteropServices.JavaScript.JSType;
using ODSaleOrder.API.Services.CaculateTax;

namespace ODSaleOrder.API.Services.SaleOrder
{
    public class ImportOrderService : IImportOrderService
    {
        // Private
        private readonly IDynamicBaseRepository<FfasoOrderInformation> _ffaSoOrderRepository;
        private readonly IDynamicBaseRepository<FfasoOrderItem> _ffaSoOrderItemRepository;
        private readonly IDynamicBaseRepository<SO_OrderInformations> _orderInformationsRepository;
        private readonly IDynamicBaseRepository<FfasoImportItem> _ffaSoImportItemRepository;
        private readonly IDynamicBaseRepository<SO_SalesOrderSetting> _settingRepository;
        private readonly IDynamicBaseRepository<INV_AllocationDetail> _allocationDetailRepo;
        private readonly IDynamicBaseRepository<InvAllocationTracking> _alocationtrackinglogRepo;
        private readonly IDynamicBaseRepository<INV_InventoryTransaction> _inventoryTransactionRepo;

        // Public
        private readonly IDynamicBaseRepository<Principal> _principalRepository;
        private readonly IDynamicBaseRepository<Kit> _kitRepository;
        private readonly IDynamicBaseRepository<Vat> _vatRepository;
        private readonly IDynamicBaseRepository<SaleCalendar> _saleCalendarRepo;
        private readonly IDynamicBaseRepository<SaleCalendarHoliday> _saleCalendarHolidayRepo;
        private readonly IDynamicBaseRepository<PrincipalWarehouseLocation> _principalWarehouseLocationRepo;

        // Service
        private readonly ILogger<ImportOrderService> _logger;
        private readonly IMapper _mapper;
        public IRestClient _clientBaseline;
        public IRestClient _clientSalesOrder;
        public IRestClient _clientInventory;
        public IRestClient _clientRdos;
        public readonly Interface.IClientService _clientService;
        public readonly ISalesOrderService _salesOrderService;
        private readonly IInventoryService _inventoryService;

        //Khoa enhance
        private readonly ICalculateTaxService _calculateTaxService;

        // Other
        private Guid ItemId = Guid.Empty;
        private Guid BaseUnit = Guid.Empty;
        private Guid Hierarchy = Guid.Empty;
        private Guid PurchaseUnit = Guid.Empty;
        private Guid SalesUnit = Guid.Empty;
        private Guid VatId = Guid.Empty;
        private Guid KitId = Guid.Empty;
        private string ItemGroupCode = null;
        //private string CustomerAttributeLasted = null;
        //private DistributorInfoModel DistributorInfo = null;
        //private CustomerSettingHierarchyModel HighestHiearchy = null;

        private bool _stockImport = true;
        private bool _stockImportAll = true;
        private string CountryPrincipal = null;

        private bool? _importBudgetStatus = true;

        public Guid FFAOrderId = Guid.Empty;

        private List<SO_OrderItems> listItemSkuNew;
        private List<PrincipalWarehouseLocation> principalLocations = new List<PrincipalWarehouseLocation>();
        private PrincipalWarehouseLocation locationDefault = null;
        private List<Vat> _listVats = new List<Vat>();

        private List<ItemMng_IventoryItemModel> _listInventoryItem = new List<ItemMng_IventoryItemModel>();
        //private List<DistributorInfoModel> _listDistributorInfo = new List<DistributorInfoModel>();
        //private List<ItemMng_InventoryItemAttribute> _listInventoryItemAttribute = new List<ItemMng_InventoryItemAttribute>();
        
        private Guid? trackingId = null;
        private List<INV_InventoryTransaction> _listInvTransactionByVisitId = new List<INV_InventoryTransaction>();
        private string _ffaVisitId = null;
        private string _createdBy = null;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _schemaName = OD_Constant.DEFAULT_SCHEMA;
        private string _distributorCode = null;

        public ImportOrderService(
            RDOSContext dataContext,
            IHttpContextAccessor httpContextAccessor,
            // Service
            ILogger<ImportOrderService> logger,
            IMapper mapper,
            Interface.IClientService clientService,
            ISalesOrderService salesOrderService,
            IInventoryService inventoryService,
            ICalculateTaxService calculateTaxService
        )
        {
            _logger = logger;
            _mapper = mapper;
            
            _clientBaseline = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.BaselineAPI).Select(x => x.Url).FirstOrDefault());
            _clientSalesOrder = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.SaleOrderAPI).Select(x => x.Url).FirstOrDefault());
            _clientInventory = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.ODInventoryAPI).Select(x => x.Url).FirstOrDefault());
            _clientRdos = new RestClient(CommonData.SystemUrl.Where(x => x.Code == CommonData.SystemUrlCode.SystemAdminAPI).Select(x => x.Url).FirstOrDefault());
            _clientService = clientService;
            _salesOrderService = salesOrderService;
            _inventoryService = inventoryService;

            // Public
            _principalRepository = new DynamicBaseRepository<Principal>(dataContext);
            _kitRepository = new DynamicBaseRepository<Kit>(dataContext);
            _vatRepository = new DynamicBaseRepository<Vat>(dataContext);
            _saleCalendarRepo = new DynamicBaseRepository<SaleCalendar>(dataContext);
            _saleCalendarHolidayRepo = new DynamicBaseRepository<SaleCalendarHoliday>(dataContext);
            _principalWarehouseLocationRepo = new DynamicBaseRepository<PrincipalWarehouseLocation>(dataContext);

            // Private
            _ffaSoOrderItemRepository = new DynamicBaseRepository<FfasoOrderItem>(dataContext);
            _ffaSoOrderRepository = new DynamicBaseRepository<FfasoOrderInformation>(dataContext);
            _orderInformationsRepository = new DynamicBaseRepository<SO_OrderInformations>(dataContext);
            _ffaSoImportItemRepository = new DynamicBaseRepository<FfasoImportItem>(dataContext);
            _settingRepository = new DynamicBaseRepository<SO_SalesOrderSetting>(dataContext);
            _allocationDetailRepo = new DynamicBaseRepository<INV_AllocationDetail>(dataContext);
            _alocationtrackinglogRepo = new DynamicBaseRepository<InvAllocationTracking>(dataContext);
            _inventoryTransactionRepo = new DynamicBaseRepository<INV_InventoryTransaction>(dataContext);


            _calculateTaxService = calculateTaxService;
          

            _httpContextAccessor = httpContextAccessor;
            _schemaName = _httpContextAccessor.HttpContext?.Items["SchemaName"] as string ?? _schemaName;
            _distributorCode = _httpContextAccessor.HttpContext?.Items["DistributorCode"] as string ?? _distributorCode;
        }

        #region Common
        // Get lead date
        private async Task<ResultModelWithObject<int>> GetLeadDate()
        {
            try
            {
                var resultData = _settingRepository
                    .GetAllQueryable(null, null, null, _schemaName)
                    .FirstOrDefault();

                if (IsODSiteConstant)
                {
                    return new ResultModelWithObject<int>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = resultData.DeliveryLeadDate
                    };
                }
                else
                {
                    return new ResultModelWithObject<int>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = resultData.LeadDate
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<int>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
        // Calculate date filter Order
        public async Task<BaseResultModel> HandleCalculateBaselineDate()
        {
            var resultBaselineDate = await GetLeadDate();
            if (!resultBaselineDate.IsSuccess)
            {
               return new BaseResultModel
               {
                   Code = resultBaselineDate.Code,
                   Message = resultBaselineDate.Message,
                   IsSuccess = false
               };
            }
            int leadDate = resultBaselineDate.Data;
            var baselineDateCurrent = DateTime.Now;
            var saleCalendar = await _saleCalendarRepo.GetAllQueryable(x => x.SaleYear == baselineDateCurrent.Year).FirstOrDefaultAsync();
            if (saleCalendar == null)
            {
                return new BaseResultModel
                {
                    Code = 400,
                    Message = "Sale Calendar not found",
                    IsSuccess = false
                };
            }
            for (int i = 1; i <= leadDate; i++)
            {
                var date = DateTime.Now.AddDays(-i);
                //var date = new DateTime(2023, 06, 26).AddDays(-i);
                if (date.Year != saleCalendar.SaleYear)
                {
                    saleCalendar = await _saleCalendarRepo.GetAllQueryable(x => x.SaleYear == date.Year).FirstOrDefaultAsync();
                }

                if ((((int)date.DayOfWeek) == 6 || ((int)date.DayOfWeek) == 0) && saleCalendar.IncludeWeekend == null)
                {
                    leadDate += 1;
                    continue;
                }
                // else if (((int)date.DayOfWeek) == 6 && saleCalendar.IncludeWeekend == "SUN")
                // {
                //     leadDate += 1;
                //     continue;
                // }
                else if (((int)date.DayOfWeek) == 0 && saleCalendar.IncludeWeekend == "SAT")
                {
                    leadDate += 1;
                    continue;
                }

                if (_saleCalendarHolidayRepo.GetAllQueryable(x => x.HolidayDate.Date == date.Date).Any())
                {
                    leadDate += 1;
                    continue;
                }
            }
            baselineDateCurrent = baselineDateCurrent.AddDays(-leadDate);

            return new BaseResultModel
            {
                IsSuccess = true,
                Message = "Success",
                Data = baselineDateCurrent,
                Code = 200
            };
        }
        // Get inventory realtime
        public async Task<ResultModelWithObject<ResultInventoryItemRealTimeModel>> GetInventoryItemRealTime(string token, string wareHouseCode, string distributorCode, string itemCode)
        {
            try
            {
                // Handle Token
                string tokenSplit = token.Split(" ").Last();

                var req = new RequestInventoryItemRealTimeModel();
                req.OrderBy = "";
                req.Filter = "";
                req.IsDropdown = true;
                req.FromDate = null;
                req.ToDate = null;
                req.SearchValue = "";
                req.SearchText = "";
                req.DistributorCode = distributorCode;
                req.WareHouseCode = wareHouseCode;
                req.ItemCode = itemCode;

                _clientInventory.Authenticator = new JwtAuthenticator($"Rdos {tokenSplit}");
                var request = new RestRequest($"AllocationItem/GetRealtimeAllocation", Method.POST, DataFormat.Json);
                request.AddJsonBody(req);
                request.AddHeader(OD_Constant.KeyHeader, _distributorCode);

                var result = _clientInventory.Execute(request);

                if (result == null || result.Content == string.Empty)
                {
                    return new ResultModelWithObject<ResultInventoryItemRealTimeModel>
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = $"Cannot GetRealtimeAllocation"
                    };
                }

                var resultData = JsonConvert.DeserializeObject<ResultModelWithObject<ResultInventoryItemRealTimeModel>>(JsonConvert.DeserializeObject(result.Content).ToString());
                return resultData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<ResultInventoryItemRealTimeModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
        // Get location
        public async Task<ResultModelWithObject<List<PrincipalWarehouseLocation>>> GetListPrincipalWarehouseLocation()
        {
            try
            {
                //List<PrincipalWarehouseLocation> listPrincipalWarehouseLocation = await _principalWarehouseLocationRepo
                //    .GetAllQueryable(x => x.DeletedDate == null
                //        && x.EffectiveFrom <= DateTime.Now &&
                //        (!x.ValidUntil.HasValue || x.ValidUntil.Value >= DateTime.Now) &&
                //        x.AllowOut)
                //    .ToListAsync();

                List<PrincipalWarehouseLocation> listPrincipalWarehouseLocation = await _principalWarehouseLocationRepo
                    .GetAllQueryable(x => x.Code == LocationCodeAllowBoook)
                    .ToListAsync();

                return new ResultModelWithObject<List<PrincipalWarehouseLocation>>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully",
                    Data = listPrincipalWarehouseLocation
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message);
                return new ResultModelWithObject<List<PrincipalWarehouseLocation>>
                {
                    Code = 500,
                    IsSuccess = false,
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }
        // Get allocation detail current
        public async Task<INV_AllocationDetail> GetAllocationDetailCurrent(string itemCode, string locationCode, string distributorCode, string wareHouseCode)
        {
            var allocatioonDetail = await _allocationDetailRepo.GetAllQueryable(null, null, null, _schemaName)
                    .FirstOrDefaultAsync(x => x.ItemCode == itemCode &&
                    x.LocationCode == locationCode &&
                    x.DistributorCode == distributorCode &&
                    x.WareHouseCode == wareHouseCode);

            return allocatioonDetail;
        }
        // Update inventory SOBooked
        public async Task<BaseResultModel> UpdateBooked(UpdateBookedAllocationModel model, BookAllocationModel req)
        {
            try
            {
                if (trackingId == null)
                {
                    trackingId = Guid.NewGuid();
                }
                var RequestId = trackingId.Value;

                if (model.SOBooked == 0)
                {
                    return new BaseResultModel
                    {
                        Code = 200,
                        IsSuccess = true,
                        Message = "Success"
                    };
                }

                #region  reuse

                //var allocatioonDetail = await _allocationDetailRepo.GetAllQueryable(null, null, null, _schemaName)
                //    .FirstOrDefaultAsync(x => x.ItemCode == model.ItemCode &&
                //    x.LocationCode == model.LocationCode &&
                //    x.DistributorCode == model.DistributorCode &&
                //    x.WareHouseCode == model.WareHouseCode);

                var allocatioonDetail = await GetAllocationDetailCurrent(model.ItemCode, model.LocationCode, model.DistributorCode, model.WareHouseCode);

                if (allocatioonDetail == null)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = $"Cannot found allocation detail with DistributorCode: {model.DistributorCode}, WarehouseCode: {model.WareHouseCode}, LocationCode: {model.LocationCode}, ItemCode: {model.ItemCode}",
                    };
                }


                if (model.SOBooked > allocatioonDetail.Available)
                {
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = $"SOBooked quantity of item code: {allocatioonDetail.ItemCode} cannot be greater than available: {allocatioonDetail.Available}"
                    };
                }

                // Tracking
                var trackingLog = new InvAllocationTracking
                {
                    Id = Guid.NewGuid(),
                    ItemKey = allocatioonDetail.ItemKey,
                    ItemId = allocatioonDetail.ItemId,
                    ItemCode = allocatioonDetail.ItemCode,
                    BaseUom = allocatioonDetail.BaseUom,
                    ItemDescription = allocatioonDetail.ItemDescription,
                    WareHouseCode = allocatioonDetail.WareHouseCode,
                    LocationCode = allocatioonDetail.LocationCode,
                    DistributorCode = allocatioonDetail.DistributorCode,
                    OnHandBeforChanged = allocatioonDetail.OnHand,
                    OnHandToChanged = 0,
                    OnHandChanged = allocatioonDetail.OnHand,
                    OnSoShippingBeforChanged = 0,
                    OnSoShippingToChanged = 0,
                    OnSoShippingChanged = 0,
                    OnSoBookedBeforChanged = allocatioonDetail.OnSoBooked,
                    OnSoBookedToChanged = model.SOBooked,
                    OnSoBookedChanged = allocatioonDetail.OnSoBooked + model.SOBooked,
                    AvailableBeforChanged = allocatioonDetail.Available,
                    AvailableToChanged = -model.SOBooked,
                    AvailableChanged = allocatioonDetail.Available - model.SOBooked,
                    ItemGroupCode = allocatioonDetail.ItemGroupCode,
                    DSACode = allocatioonDetail.DSACode,
                    FromFeature = "FFAImport",
                    RequestDate = DateTime.Now,
                    RequestId = RequestId,
                    IsSuccess = true,
                    RequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(model)
                };

                allocatioonDetail.OnSoBooked += model.SOBooked;
                allocatioonDetail.Available -= model.SOBooked;
                allocatioonDetail.UpdatedDate = DateTime.Now;
                allocatioonDetail.UpdatedBy = "System";
                _alocationtrackinglogRepo.Add(trackingLog, _schemaName);

                if (allocatioonDetail.OnSoBooked < 0)
                {
                    //Detach all tracking entites
                    _alocationtrackinglogRepo.DetachEntity(_schemaName);
                    trackingLog.IsSuccess = false;
                    //Save only failed trackingLog
                    _alocationtrackinglogRepo.Add(trackingLog, _schemaName);
                    _alocationtrackinglogRepo.Save(_schemaName);
                    return new BaseResultModel
                    {
                        IsSuccess = false,
                        Code = 400,
                        Message = $"SOBooked Booked quantity result of item code: {allocatioonDetail.ItemCode} cannot be less than 0. Booking quantities: {model.SOBooked}, Result: {allocatioonDetail.OnSoBooked}"
                    };
                }

                // Save transaction
                INV_InventoryTransaction transactionInv = new();
                transactionInv.Id = Guid.NewGuid();
                transactionInv.ItemId = allocatioonDetail.ItemId;
                transactionInv.ItemCode = allocatioonDetail.ItemCode;
                transactionInv.ItemDescription = allocatioonDetail.ItemDescription;
                transactionInv.Uom = req.BookUom;
                transactionInv.Quantity = req.BookQty;
                transactionInv.BaseQuantity = req.BookBaseQty;
                transactionInv.OrderBaseQuantity = req.BookBaseQty;
                transactionInv.TransactionDate = DateTime.Now;
                transactionInv.TransactionType = INV_TransactionType.SO_CONFIRM;
                transactionInv.WareHouseCode = allocatioonDetail.WareHouseCode;
                transactionInv.LocationCode = allocatioonDetail.LocationCode;
                transactionInv.DistributorCode = allocatioonDetail.DistributorCode;
                transactionInv.OrderCode = null;
                transactionInv.ItemKey = allocatioonDetail.ItemKey;
                transactionInv.BegQty = allocatioonDetail.OnHand;
                transactionInv.EndQty = transactionInv.BegQty;
                transactionInv.IsDeleted = false;
                transactionInv.CreatedBy = req.CreatedBy;
                transactionInv.CreatedDate = DateTime.Now;
                transactionInv.FFAVisitId = req.FFAVisitID;
                transactionInv.OneShopId = req.OneShopID;
                transactionInv.ItemGroupCode = req.ItemGroupCode;
                transactionInv.Priority = req.Priority;
                transactionInv.IsCreateOrderItem = true;
                transactionInv.IsCreateInFlow = true;
                transactionInv.OwnerCode = _distributorCode;
                transactionInv.OwnerType = OwnerTypeConstant.DISTRIBUTOR;
                transactionInv.Source = SO_SOURCE_CONST.MOBILE;
                transactionInv.OrderLineId = req.OrderLineId;
                transactionInv.OrderType = req.OrderType;

                _inventoryTransactionRepo.Add(transactionInv, _schemaName);
                _listInvTransactionByVisitId.Add(transactionInv);

                _allocationDetailRepo.UpdateUnSaved(allocatioonDetail, _schemaName);
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Successfully",
                };
                #endregion
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
        // Send notification
        public async Task<BaseResultModel> NotifyMobileOrderImportFailed(string token, SendNotifiMobileModel dataInput)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(token.Split(" ").Last().Trim());

                var moduleToken = jwtSecurityToken.Claims.FirstOrDefault(x => x.Type == CustomClaimType.ModuleToken)?.Value;
                //var deviceTokens = (_clientService.CommonRequest<ResultModelWithObject<DeviceTokenEmployeeModel>>(CommonData.SystemUrlCode.NotiMobileRdosAPI, $"notification/getusertoken/{dataInput.EmployeeCode}", Method.POST, moduleToken, null)).Data;

                var dataRequest = new NotificationRequestModel();
                dataRequest.Title = "Đơn hàng cần kiểm tra lại";
                dataRequest.Body = $"Đơn hàng {dataInput.ExternalNumber} xử lý vào RDOS không thành công. Vui lòng kiểm tra lại đơn hàng chờ xác nhận lại";
                dataRequest.Type = 2;
                //dataRequest.deviceToken = deviceTokens.deviceToken;
                dataRequest.Purpose = "SONEEDCONFIRM";
                dataRequest.IsUrgent = true;
                dataRequest.Priority = "WARNING";
                dataRequest.NavigatePath = null;
                dataRequest.DataId = dataInput.ExternalNumber;
                dataRequest.SyncCode = null;
                dataRequest.Status = null;
                dataRequest.Action = "info";
                dataRequest.NotificationType = "URGENT";
                dataRequest.TemplateData = $"External_OrdNBR={dataInput.ExternalNumber};CustomerName={dataInput.CustomerName}";
                dataRequest.EmployeeCode = dataInput.EmployeeCode;

                var result = _clientService.CommonRequest<BaseResultModel>(CommonData.SystemUrlCode.NotiMobileRdosAPI, $"notification/pushnotication", Method.POST, moduleToken, dataRequest);
                return new BaseResultModel()
                {
                    Code = 200,
                    Message = "Success",
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
        #endregion

        #region FFA
        // Get list order FFA
        public async Task<ResultModelWithObject<ListFfaModel>> GetListOrderFfa(SearchFfaOrderModel req)
        {
            try
            {
                if (!req.FromDate.HasValue)
                {
                    var baselineDateCurrent = DateTime.Now;

                    var resultDate = await HandleCalculateBaselineDate();
                    if (!resultDate.IsSuccess)
                    {
                        return new ResultModelWithObject<ListFfaModel>()
                        {
                            Code = resultDate.Code,
                            Message = resultDate.Message,
                            IsSuccess = false
                        };
                    }

                    baselineDateCurrent = (DateTime)resultDate.Data;
                    req.FromDate = baselineDateCurrent;
                }

                var listDistributorCode = new List<string>();
                var listShiptoCode = new List<string>();

                if (req.ListDistributorAndShipto.Count > 0)
                {
                    foreach (var reqDetail in req.ListDistributorAndShipto)
                    {
                        listDistributorCode.Add(reqDetail.DistributorCode);
                        listShiptoCode.Add(reqDetail.ShiptoCode);
                    }
                }

                //var listOrder = new List<FfaOrderDetailModel>();
                IQueryable<FfaOrderDetailModel> listOrder = null;
                if (listDistributorCode.Count > 0 && listShiptoCode.Count > 0)
                {
                    listOrder = from header in _ffaSoOrderRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking()

                                //join detail in _ffaSoOrderItemRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking()
                                //on header.External_OrdNBR equals detail.External_OrdNBR into data
                                //from detail in data.DefaultIfEmpty()

                                //join detailImport in _ffaSoImportItemRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking()
                                //on header.External_OrdNBR equals detailImport.External_OrdNBR into dataImport
                                //from detailImport in dataImport.DefaultIfEmpty()

                                where ((header.IsDeleted.HasValue && !header.IsDeleted.Value) || !header.IsDeleted.HasValue) &&
                                    //(header.OrderDate.HasValue && header.OrderDate.Value.Date >= baselineDateCurrent.Date) &&
                                    header.OrderType != FFA_ORDER_TYPE.DirectOrder &&
                                    (!string.IsNullOrEmpty(header.WareHouseID) && listShiptoCode.Contains(header.WareHouseID)) &&
                                    (!string.IsNullOrEmpty(header.DistributorCode) && listDistributorCode.Contains(header.DistributorCode)) &&
                                    ((!string.IsNullOrWhiteSpace(header.ImportStatus) && header.ImportStatus == IMPORT_STATUS.FAILED) ||
                                    string.IsNullOrWhiteSpace(header.ImportStatus))
                                select new FfaOrderDetailModel
                                {
                                    OrderInfo = header,
                                    //Item = detail,
                                    //ItemImport = detailImport
                                };
                }
                else
                {
                    listOrder = from header in _ffaSoOrderRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking()

                                //join detail in _ffaSoOrderItemRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking()
                                //on header.External_OrdNBR equals detail.External_OrdNBR into data
                                //from detail in data.DefaultIfEmpty()

                                //join detailImport in _ffaSoImportItemRepository.GetAllQueryable(null, null, null, _schemaName).AsNoTracking()
                                //on header.External_OrdNBR equals detailImport.External_OrdNBR into dataImport
                                //from detailImport in dataImport.DefaultIfEmpty()

                                where ((header.IsDeleted.HasValue && !header.IsDeleted.Value) || !header.IsDeleted.HasValue) &&
                                //(header.OrderDate.HasValue && header.OrderDate.Value.Date >= baselineDateCurrent.Date) &&
                                header.OrderType != FFA_ORDER_TYPE.DirectOrder &&
                                ((!string.IsNullOrWhiteSpace(header.ImportStatus) && header.ImportStatus == IMPORT_STATUS.FAILED) ||
                                string.IsNullOrWhiteSpace(header.ImportStatus))
                                select new FfaOrderDetailModel
                                {
                                    OrderInfo = header,
                                    //Item = detail,
                                    //ItemImport = detailImport
                                };
                }

                if (!string.IsNullOrWhiteSpace(req.SalesRepID))
                {
                    listOrder = listOrder.Where(x =>
                        (!string.IsNullOrWhiteSpace(x.OrderInfo.SalesRepID) && x.OrderInfo.SalesRepID.ToLower().Trim().Contains(req.SalesRepID.ToLower().Trim())));
                }

                if (!string.IsNullOrWhiteSpace(req.Customer))
                {
                    listOrder = listOrder.Where(x =>
                        !string.IsNullOrWhiteSpace(x.OrderInfo.CustomerId) && x.OrderInfo.CustomerId.ToLower().Trim().Contains(req.Customer.ToLower().Trim()) ||
                        !string.IsNullOrWhiteSpace(x.OrderInfo.CustomerName) && x.OrderInfo.CustomerName.ToLower().Trim().Contains(req.Customer.ToLower().Trim()));
                }

                if (!string.IsNullOrWhiteSpace(req.ImportStatus))
                {
                    if (req.ImportStatus.ToString() == IMPORT_STATUS.NULL)
                    {
                        listOrder = listOrder.Where(x => string.IsNullOrWhiteSpace(x.OrderInfo.ImportStatus));
                    }
                    else
                    {
                        listOrder = listOrder.Where(x =>
                            (!string.IsNullOrWhiteSpace(x.OrderInfo.ImportStatus) && x.OrderInfo.ImportStatus.ToLower().Trim().Contains(req.ImportStatus.ToLower().Trim())));
                    }
                }

                if (!string.IsNullOrWhiteSpace(req.FFAStatus))
                {
                    listOrder = listOrder.Where(x =>
                            (!string.IsNullOrWhiteSpace(x.OrderInfo.Status) && x.OrderInfo.Status.ToLower() == req.FFAStatus.ToLower()));
                }

                if (req.FromDate.HasValue)
                {
                    listOrder = listOrder.Where(x => x.OrderInfo.OrderDate.Value.Date >= req.FromDate.Value.Date);
                }
                if (req.ToDate.HasValue)
                {
                    listOrder = listOrder.Where(x => x.OrderInfo.OrderDate.Value.Date <= req.ToDate.Value.Date);
                }

                var listDataRes = new List<FfaOrderGroupModel>();

                if (req.IsDropdown)
                {
                    var listOrderGroup = listOrder.AsEnumerable().GroupBy(x => x.OrderInfo.External_OrdNBR).Select(x => x.First()).ToList();
                    foreach (var orderDetail in listOrderGroup)
                    {
                        var dataRes = new FfaOrderGroupModel();
                        dataRes.OrderInfo = orderDetail.OrderInfo;
                        //var listItemDetail = listOrder.Where(x => !string.IsNullOrEmpty(x.Item.External_OrdNBR) && x.Item.External_OrdNBR == orderDetail.OrderInfo.External_OrdNBR).Select(x => x.Item).ToList();
                        dataRes.Items = await _ffaSoOrderItemRepository
                            .GetAllQueryable(x => 
                                !string.IsNullOrEmpty(x.External_OrdNBR) && x.External_OrdNBR == orderDetail.OrderInfo.External_OrdNBR 
                                && !string.IsNullOrEmpty(x.OrderType) && x.OrderType == orderDetail.OrderInfo.OrderType
                                ,null, null, _schemaName)
                            .AsNoTracking().OrderByDescending(x => x.CreatedDate)
                            .ToListAsync();
                        //var listItemDetailImport = listOrder.Where(x => x.ItemImport != null && !string.IsNullOrEmpty(x.ItemImport.External_OrdNBR) && !x.ItemImport.IsDeleted && x.ItemImport.External_OrdNBR == orderDetail.OrderInfo.External_OrdNBR).Select(x => x.ItemImport).ToList();
                        //dataRes.ItemImports = await _ffaSoImportItemRepository.GetAllQueryable(x => !string.IsNullOrEmpty(x.External_OrdNBR) && !x.IsDeleted && x.External_OrdNBR == orderDetail.OrderInfo.External_OrdNBR, null, null, _schemaName).AsNoTracking().ToListAsync();
                        listDataRes.Add(dataRes);
                    }

                    var page1 = PagedList<FfaOrderGroupModel>.ToPagedList(listDataRes, 0, listDataRes.Count);

                    var reponse = new ListFfaModel { Items = listDataRes, MetaData = page1.MetaData };
                    return new ResultModelWithObject<ListFfaModel>
                    {
                        IsSuccess = true,
                        Code = 200,
                        Message = "Success",
                        Data = reponse
                    };
                }
                else
                {
                    int totalCount = listOrder.Count();
                    int skip = (req.PageNumber - 1) * req.PageSize;
                    int top = req.PageSize;
                    var items = listOrder.Skip(skip).Take(top).ToList();

                    var listOrderGroup = items.GroupBy(x => x.OrderInfo.External_OrdNBR).Select(x => x.First()).ToList();
                    foreach (var orderDetail in listOrderGroup)
                    {
                        var dataRes = new FfaOrderGroupModel();
                        dataRes.OrderInfo = orderDetail.OrderInfo;
                        //var listItemDetail = listOrder.Where(x => !string.IsNullOrEmpty(x.Item.External_OrdNBR) && x.Item.External_OrdNBR == orderDetail.OrderInfo.External_OrdNBR).Select(x => x.Item).ToList();
                        dataRes.Items = await _ffaSoOrderItemRepository
                            .GetAllQueryable(x => 
                                !string.IsNullOrEmpty(x.External_OrdNBR) && x.External_OrdNBR == orderDetail.OrderInfo.External_OrdNBR
                                && !string.IsNullOrEmpty(x.OrderType) && x.OrderType == orderDetail.OrderInfo.OrderType
                                , null, null, _schemaName).AsNoTracking().OrderByDescending(x => x.CreatedDate).ToListAsync();

                        //var listItemDetailImport = listOrder.Where(x => x.ItemImport != null && !string.IsNullOrEmpty(x.ItemImport.External_OrdNBR) && !x.ItemImport.IsDeleted && x.ItemImport.External_OrdNBR == orderDetail.OrderInfo.External_OrdNBR).Select(x => x.ItemImport).ToList();
                        //dataRes.ItemImports = await _ffaSoImportItemRepository.GetAllQueryable(x => !string.IsNullOrEmpty(x.External_OrdNBR) && !x.IsDeleted && x.External_OrdNBR == orderDetail.OrderInfo.External_OrdNBR, null, null, _schemaName).AsNoTracking().ToListAsync();
                        listDataRes.Add(dataRes);
                    }

                    var result = new PagedList<FfaOrderGroupModel>(listDataRes, totalCount, (skip / top) + 1, top);

                    var repsonse = new ListFfaModel { Items = result, MetaData = result.MetaData };

                    //return metadata
                    return new ResultModelWithObject<ListFfaModel>
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
                return new ResultModelWithObject<ListFfaModel>
                {
                    IsSuccess = false,
                    Code = 500,
                    Message = ex.InnerException?.Message ?? ex.Message,
                };
            }
        }
        // Get detail
        public async Task<ResultModelWithObject<FfaOrderGroupModel>> GetDetailFfaOrder(string orderNumber)
        {
            try
            {
                var ffaOrderHeader = await _ffaSoOrderRepository.GetAllQueryable(null, null, null, _schemaName)
                    .FirstOrDefaultAsync(x => x.External_OrdNBR == orderNumber && x.OrderType != FFA_ORDER_TYPE.DirectOrder &&
                    ((!string.IsNullOrWhiteSpace(x.ImportStatus) && x.ImportStatus == IMPORT_STATUS.FAILED) || string.IsNullOrWhiteSpace(x.ImportStatus)) &&
                    ((x.IsDeleted.HasValue && !x.IsDeleted.Value) || !x.IsDeleted.HasValue));

                if (ffaOrderHeader == null)
                {
                    return new ResultModelWithObject<FfaOrderGroupModel>
                    {
                        IsSuccess = false,
                        Code = 404,
                        Message = $"Cannot found order number: {orderNumber}"
                    };
                }

                var ffaOrderItems = await _ffaSoOrderItemRepository.GetAllQueryable(null, null, null, _schemaName)
                    .Where(x => x.External_OrdNBR == orderNumber && x.OrderType == ffaOrderHeader.OrderType && ((x.IsDeleted.HasValue && !x.IsDeleted.Value) || !x.IsDeleted.HasValue)).ToListAsync();

                //var ffaOrderImportItems = await _ffaSoImportItemRepository.GetAllQueryable(null, null, null, _schemaName)
                //    .Where(x => x.External_OrdNBR == orderNumber && !x.IsDeleted).ToListAsync();

                var dataRes = new FfaOrderGroupModel();
                dataRes.OrderInfo = ffaOrderHeader;
                dataRes.Items = ffaOrderItems;
                //dataRes.ItemImports = ffaOrderImportItems;

                return new ResultModelWithObject<FfaOrderGroupModel>
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = dataRes
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.InnerException?.Message ?? ex.Message + " " + ex.StackTrace);
                return new ResultModelWithObject<FfaOrderGroupModel>
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
                var group = listTransaction.GroupBy(x => new { x.FFAVisitId, x.ItemCode }).Select(x => x.First()).ToList();
                foreach (var item in group)
                {
                    var totalLine = listTransaction.Where(x => x.FFAVisitId == item.FFAVisitId && x.ItemCode == item.ItemCode).ToList();
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
                            Message = $"Error with Order FFAVisitID {item.FFAVisitId}. The number of books is negative of item {item.ItemCode}, Group {item.ItemGroupCode}",
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

        // Handle book item
        public async Task<BaseResultModel> HandleBookInventoryV2(FfasoOrderItem item, string distributorCode, string wareHouseCode, string token, List<FfasoImportItem> listItemImport)
        {
            try
            {
                // Kiểm tra nếu UnitRate null thì trả lỗi không book nữa
                if(item.UnitRate == 0 && item.UOM != item.BaseUnitCode) 
                {
                    return new BaseResultModel{
                        IsSuccess = false,
                        Message = $"Error with Order {item.External_OrdNBR}. Invalid UnitRate of item {item.ItemCode} , Group {item.ItemGroupCode} , type {item.AllocateType}"
                    };
                }

                // Handle số cần book
                if (item.OriginalOrderQtyBooked == null) item.OriginalOrderQtyBooked = 0;
                // Số book mong muốn hiện tại
                int _wantedBookBaseQty = item.OriginalOrderBaseQty.HasValue ? item.OriginalOrderBaseQty.Value : 0;
                // Số đã book trước đó
                int _bookedSalesQty = item.OriginalOrderQtyBooked.HasValue ? item.OriginalOrderQtyBooked.Value : 0;
                // Số đã book trước đó, được dùng trong flow update booked inventory
                var remainbaseQty = _wantedBookBaseQty - (int)(_bookedSalesQty * item.UnitRate);
                // Số sales reamin sẽ cần phải book
                var remainNeedBook = (int)(remainbaseQty / item.UnitRate);
                var remainBooked = 0;

                // Flow book SKU
                if (item.AllocateType.ToUpper() == AllocateType.SKU.ToUpper() ||
                    item.AllocateType.ToUpper() == AllocateType.KIT.ToUpper())
                {
                    if (item.ItemCode == null || string.IsNullOrEmpty(item.ItemCode))
                    {
                        return new BaseResultModel()
                        {
                            IsSuccess = false,
                            Message = $"Cannot found item code in order code: {item.External_OrdNBR}",
                            Code = 404
                        };
                    }

                    // Đã book trước đó
                    if (_bookedSalesQty != 0)
                    {
                        // Handle transaction
                        INV_InventoryTransaction transactionCheck = _listInvTransactionByVisitId
                            .Where(x => x.ItemCode == item.ItemCode && !x.IsCreateOrderItem && x.OrderLineId == item.Id.ToString())
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
                    

                    // Get allocation inventory realtime
                    var result = await GetInventoryItemRealTime(token, wareHouseCode, distributorCode, item.ItemCode);
                    if (!result.IsSuccess)
                    {
                        return new BaseResultModel()
                        {
                            IsSuccess = false,
                            Message = result.Message,
                            Code = result.Code
                        };
                    }

                    // Order by descending available
                    var listAllocationItem = result.Data.Items.OrderByDescending(x => x.Available).ToList();

                    // Lấy allocation có location default
                    var allocationDetail = new INV_AllocationDetailModel();

                    var detailSKU = _listInventoryItem.FirstOrDefault(x => x.InventoryItem.InventoryItemId == item.ItemCode);

                    // Nếu detail SKU null
                    if (detailSKU == null)
                    {
                        // Get detail inventory item
                        detailSKU = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryItemByCode/{item.ItemCode}", Method.GET, $"Rdos {token.Split(" ").Last()}", null);
                        _listInventoryItem.Add(detailSKU);
                    }


                    // Đắp data cho item của SOOrderItems
                    ItemId = detailSKU.InventoryItem.Id;
                    BaseUnit = detailSKU.InventoryItem.BaseUnit;
                    PurchaseUnit = detailSKU.InventoryItem.PurchaseUnit;
                    SalesUnit = detailSKU.InventoryItem.SalesUnit;
                    VatId = detailSKU.InventoryItem.Vat;
                    Hierarchy = detailSKU.InventoryItem.Hierarchy;
                    ItemGroupCode = detailSKU.InventoryItem.GroupId;
                    KitId = Guid.Empty;


                    if (item.AllocateType.ToUpper() == AllocateType.KIT.ToUpper())
                    {
                        // Get detail KIT
                        var kitInDb = await _kitRepository.GetAllQueryable().AsNoTracking().FirstOrDefaultAsync(x => x.ItemKitId == item.KitId);
                        KitId = kitInDb != null ? kitInDb.Id : Guid.Empty;
                    }


                    foreach (var location in principalLocations.OrderByDescending(x => x.IsDefault))
                    {
                        if (remainNeedBook <= 0)
                        {
                            break;
                        }

                        allocationDetail = listAllocationItem.FirstOrDefault(x => x.LocationCode == location.Code.ToString());
                        if (allocationDetail == null) continue;

                        INV_AllocationDetail allocationDetailCurrent = await GetAllocationDetailCurrent(allocationDetail.ItemCode, allocationDetail.LocationCode, allocationDetail.DistributorCode, allocationDetail.WareHouseCode);
                        if (allocationDetailCurrent != null)
                        {
                            allocationDetail.OnSoBooked = allocationDetailCurrent.OnSoBooked;
                            allocationDetail.Available = allocationDetailCurrent.Available;
                        }

                        //handleBooked
                        // Số sales qty có thể booked
                        var allocationAllowToBook = (int)(allocationDetail.Available / item.UnitRate);

                        // check allocation khi quy ra sales phải ít nhất là 1
                        if (allocationAllowToBook > 1)
                        {
                            var SalesBookedQty = allocationAllowToBook > remainNeedBook ? remainNeedBook : allocationAllowToBook;

                            if (location.IsDefault && remainNeedBook - SalesBookedQty == 0) //nếu là default và book đủ trong lần đầu => là phần từ đầu tiên => pass
                            {
                                item.LocationID = allocationDetail.LocationCode;
                                // Flow update booked inventory
                                var request = new UpdateBookedAllocationModel
                                {
                                    DistributorCode = distributorCode,
                                    WareHouseCode = wareHouseCode,
                                    LocationCode = allocationDetail.LocationCode,
                                    ItemCode = item.ItemCode,
                                    SOBooked = (int)(SalesBookedQty * item.UnitRate), //Quy lại base
                                };

                                BookAllocationModel reqBook = new();
                                reqBook.OrderID = FFAOrderId;
                                reqBook.OneShopID = null;
                                reqBook.FFAVisitID = _ffaVisitId;
                                reqBook.CreatedBy = _createdBy;
                                reqBook.BookBaseQty = request.SOBooked;
                                reqBook.BookQty = SalesBookedQty;
                                reqBook.BookUom = item.UOM;
                                reqBook.ItemGroupCode = null;
                                reqBook.Priority = 0;
                                reqBook.OrderLineId = item.Id.ToString();
                                reqBook.OrderType = item.OrderType;

                                var resultBooked = await UpdateBooked(request, reqBook);
                                if (!resultBooked.IsSuccess) return resultBooked;

                                // Cập nhật Số booked mới
                                item.OriginalOrderQtyBooked += SalesBookedQty;
                            }
                            else if (SalesBookedQty > 0)
                            {
                                var itemNew = mappingItemFromFFaItem(item);
                                itemNew.LocationID = allocationDetail.LocationCode;
                                itemNew.OrderQuantities = SalesBookedQty;
                                itemNew.UOM = item.UOM;

                                itemNew.Orig_Ord_Line_Amt = itemNew.UnitPrice * itemNew.OrderQuantities;
                                itemNew.Ord_Line_Amt = itemNew.UnitPrice * itemNew.OrderQuantities;
                                itemNew.Shipped_Line_Amt = 0;

                                if (item.Orig_Ord_line_Disc_Amt.HasValue && item.OriginalOrderQty.HasValue)
                                {
                                    itemNew.Orig_Ord_line_Disc_Amt = (decimal)item.Orig_Ord_line_Disc_Amt.Value * itemNew.OrderQuantities / (decimal)item.OriginalOrderQty.Value;
                                }
                                else
                                {
                                    itemNew.Orig_Ord_line_Disc_Amt = 0;
                                }

                                itemNew.Ord_line_Disc_Amt = itemNew.Orig_Ord_line_Disc_Amt;
                                itemNew.Shipped_line_Disc_Amt = 0;
                                itemNew.Orig_Ord_Line_Extend_Amt = itemNew.Orig_Ord_Line_Amt - itemNew.Orig_Ord_line_Disc_Amt;
                                itemNew.Ord_Line_Extend_Amt = itemNew.Ord_Line_Amt - itemNew.Ord_line_Disc_Amt;
                                itemNew.Shipped_Line_Extend_Amt = 0;
                                itemNew.OriginalOrderQuantities = itemNew.OrderQuantities;
                                itemNew.OriginalOrderBaseQuantities = itemNew.OrderBaseQuantities;

                                var requestBook = new UpdateBookedAllocationModel
                                {
                                    DistributorCode = distributorCode,
                                    WareHouseCode = wareHouseCode,
                                    LocationCode = allocationDetail.LocationCode,
                                    ItemCode = item.ItemCode,
                                    SOBooked = (int)(SalesBookedQty * item.UnitRate), //Quy lại base,
                                };

                                BookAllocationModel reqBook = new();
                                reqBook.OrderID = FFAOrderId;
                                reqBook.OneShopID = null;
                                reqBook.FFAVisitID = _ffaVisitId;
                                reqBook.CreatedBy = _createdBy;
                                reqBook.BookBaseQty = requestBook.SOBooked;
                                reqBook.BookQty = SalesBookedQty;
                                reqBook.BookUom = item.UOM;
                                reqBook.ItemGroupCode = null;
                                reqBook.Priority = 0;
                                reqBook.OrderLineId = item.Id.ToString();
                                reqBook.OrderType = item.OrderType;
                                var resBooked = await UpdateBooked(requestBook, reqBook);

                                if (!resBooked.IsSuccess) return resBooked;
                                // Cập nhật Số booked mới
                                item.OriginalOrderQtyBooked += SalesBookedQty;

                                listItemSkuNew.Add(itemNew);

                                //đánh 2 cờ Need Confirm
                                _stockImport = false;
                                _stockImportAll = false;
                            }
                            //Tính lại số remain cần book
                            remainNeedBook -= SalesBookedQty;
                            remainBooked += SalesBookedQty;
                        }

                    }
                }
                // Line item is Group or Attribute
                else if (item.AllocateType.ToUpper() == AllocateType.GROUP.ToUpper() ||
                        item.AllocateType.ToUpper() == AllocateType.ATTRIBUTE.ToUpper())
                {
                    // Khởi tạo danh sách các item SKU được tách từ ItemGroup
                    listItemSkuNew = new List<SO_OrderItems>();

                    // Handle transaction
                    List<INV_InventoryTransaction> listInvTransactionByItemGroup = _listInvTransactionByVisitId
                        .Where(x => x.ItemGroupCode == item.ItemGroupCode && x.OrderLineId == item.Id.ToString())
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

                    // TH1: Đã book kho ItemGroup 1 phần
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
                            reqGetRealtimeAllocation.LocationCode = LocationCodeAllowBoook.ToString();
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
                                    var detailSKU = _listInventoryItem.FirstOrDefault(x => x.InventoryItem.InventoryItemId == tranDetailGroup.ItemCode);
                                    if (detailSKU == null)
                                    {
                                        detailSKU = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryItemByCode/{tranDetailGroup.ItemCode}", Method.GET, $"Rdos {token.Split(" ").Last()}", null);
                                        _listInventoryItem.Add(detailSKU);
                                    }

                                    // Get attribute code của SKU
                                    var detailSelectedItem = _clientService.CommonRequest<ItemMng_InventoryItemAttribute>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryAttributeByCode/{tranDetailGroup.ItemCode}", Method.GET, $"Rdos {token.Split(" ").Last()}", null);

                                    // Không tìm thấy data detail thì skip
                                    if (detailSKU == null || detailSelectedItem == null)
                                    {
                                        //đánh 2 cờ Need Confirm
                                        _stockImport = false;
                                        _stockImportAll = false;
                                        continue;
                                    }

                                    var requestBook = new UpdateBookedAllocationModel
                                    {
                                        DistributorCode = distributorCode,
                                        WareHouseCode = wareHouseCode,
                                        LocationCode = LocationCodeAllowBoook.ToString(),
                                        ItemCode = tranDetailGroup.ItemCode,
                                        SOBooked = BaseBookedQty,
                                    };

                                    // Handle book kho
                                    BookAllocationModel reqBook = new();
                                    reqBook.OrderID = FFAOrderId;
                                    reqBook.OneShopID = null;
                                    reqBook.FFAVisitID = _ffaVisitId;
                                    reqBook.CreatedBy = _createdBy;
                                    reqBook.BookBaseQty = BaseBookedQty;
                                    reqBook.BookQty = remainNeedBook;
                                    reqBook.BookUom = item.UOM;
                                    reqBook.ItemGroupCode = item.ItemGroupCode;
                                    reqBook.Priority = count;
                                    reqBook.OrderLineId = item.Id.ToString();
                                    reqBook.OrderType = item.OrderType;

                                    // Cập nhật Số booked mới
                                    item.OriginalOrderQtyBooked += remainNeedBook;
                                    var resBooked = await UpdateBooked(requestBook, reqBook);

                                    // Nếu item đã được book trước đó thì tạo thành item của đơn SO phải + lại
                                    int _BookedQty = remainNeedBook;
                                    int _BookedBaseQty = BaseBookedQty;
                                    foreach (var tranDetail in listInvTransactionByItemGroup.Where(x => x.ItemCode == detailSKU.InventoryItem.InventoryItemId).ToList())
                                    {
                                        _BookedQty += tranDetail.Quantity;
                                        _BookedBaseQty += tranDetail.BaseQuantity;
                                        tranDetail.IsCreateOrderItem = true;
                                    }

                                    var itemNew = mappingItemFromFFaItem(item);
                                    itemNew.OriginalOrderQuantities = _BookedQty;
                                    itemNew.OriginalOrderBaseQuantities = _BookedBaseQty;
                                    itemNew.OrderQuantities = _BookedQty;
                                    itemNew.OrderBaseQuantities = _BookedBaseQty;
                                    //Bổ sung thuộc tính cho Item
                                    itemNew.Id = Guid.NewGuid();
                                    itemNew.ItemCode = tranDetailGroup.ItemCode;
                                    itemNew.InventoryID = tranDetailGroup.ItemCode;
                                    itemNew.ItemDescription = tranDetailGroup.ItemDescription;
                                    itemNew.ItemGroupCode = tranDetailGroup.ItemGroupCode;
                                    itemNew.BaseUnit = detailSKU.InventoryItem.BaseUnit;
                                    itemNew.BaseUnitCode = detailSelectedItem.BaseUOMCode;
                                    itemNew.BaseUomCode = detailSelectedItem.BaseUOMCode;
                                    itemNew.ItemId = detailSKU.InventoryItem.Id;
                                    itemNew.PurchaseUnit = detailSKU.InventoryItem.PurchaseUnit;
                                    itemNew.PurchaseUnitCode = detailSelectedItem.PurchaseUOMCode;
                                    itemNew.SalesUnit = detailSKU.InventoryItem.SalesUnit;
                                    itemNew.SalesUnitCode = detailSelectedItem.SalesUOMCode;
                                    itemNew.VatId = detailSKU.InventoryItem.Vat;
                                    itemNew.Hierarchy = detailSKU.InventoryItem.Hierarchy;
                                    itemNew.InventoryAttibute1 = detailSelectedItem.AttributeCode1;
                                    itemNew.InventoryAttibute2 = detailSelectedItem.AttributeCode2;
                                    itemNew.InventoryAttibute3 = detailSelectedItem.AttributeCode3;
                                    itemNew.InventoryAttibute4 = detailSelectedItem.AttributeCode4;
                                    itemNew.InventoryAttibute5 = detailSelectedItem.AttributeCode5;
                                    itemNew.InventoryAttibute6 = detailSelectedItem.AttributeCode6;
                                    itemNew.InventoryAttibute7 = detailSelectedItem.AttributeCode7;
                                    itemNew.InventoryAttibute8 = detailSelectedItem.AttributeCode8;
                                    itemNew.InventoryAttibute9 = detailSelectedItem.AttributeCode9;
                                    itemNew.InventoryAttibute10 = detailSelectedItem.AttributeCode10;
                                    itemNew.LocationID = LocationCodeAllowBoook.ToString();
                                    item.LocationID = LocationCodeAllowBoook.ToString();

                                    //THông tin order
                                    itemNew.LocationID = LocationCodeAllowBoook.ToString();
                                    itemNew.UOM = item.UOM;

                                    // handle data Price
                                    itemNew.Orig_Ord_Line_Amt = itemNew.UnitPrice * itemNew.OrderQuantities;
                                    itemNew.Ord_Line_Amt = itemNew.UnitPrice * itemNew.OrderQuantities;
                                    itemNew.Shipped_Line_Amt = 0;

                                    if (item.Orig_Ord_line_Disc_Amt.HasValue && item.OriginalOrderQty.HasValue)
                                    {
                                        itemNew.Orig_Ord_line_Disc_Amt = (decimal)item.Orig_Ord_line_Disc_Amt.Value * itemNew.OrderQuantities / (decimal)item.OriginalOrderQty.Value;
                                    }
                                    else
                                    {
                                        itemNew.Orig_Ord_line_Disc_Amt = 0;
                                    }

                                    itemNew.Ord_line_Disc_Amt = itemNew.Orig_Ord_line_Disc_Amt;
                                    itemNew.Shipped_line_Disc_Amt = 0;
                                    itemNew.Orig_Ord_Line_Extend_Amt = itemNew.Orig_Ord_Line_Amt - itemNew.Orig_Ord_line_Disc_Amt;
                                    itemNew.Ord_Line_Extend_Amt = itemNew.Ord_Line_Amt - itemNew.Ord_line_Disc_Amt;
                                    itemNew.Shipped_Line_Extend_Amt = 0;
                                    listItemSkuNew.Add(itemNew);

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

                    // TH2: Chưa book kho ItemGroup lần nào
                    if (!isCheckSuccess)
                    {
                        // Get item SKU và Số lượng cần book hiện tại
                        var standarBookRequest = new StandardRequestModel()
                        {
                            DistributorCode = distributorCode,
                            DistributorShiptoCode = wareHouseCode,
                            ItemGroupCode = item.ItemGroupCode,
                            Quantity = _wantedBookBaseQty,
                        };

                        var standardBookSkus = _clientService.CommonRequest<List<StandardItemModel>>(
                                CommonData.SystemUrlCode.ODItemAPI,
                                $"Standard/GetStockInventoryItemStdByItemGroupVer2",
                                Method.POST,
                                $"Rdos {token.Split(" ").Last()}",
                                standarBookRequest
                               );

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
                            //var listItemImportFfa = new List<FfasoImportItem>();
                            // Loop SKU
                            foreach (var skuStandar in standardBookSkus)
                            {
                                count += 1;
                                //Nếu remain không còn => break
                                if (remainNeedBook < 1) break;

                                //Sales Qty của sku
                                //var skuSalesAvailableQty = (int)(skuStandar.Avaiable / item.UnitRate);
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

                                //Lấy kho
                                // Get allocation inventory
                                INV_AllocationDetail allocationDetailCurrent = await GetAllocationDetailCurrent(skuStandar.InventoryCode, LocationCodeAllowBoook.ToString(), distributorCode, wareHouseCode);

                                //// Get allocation detail current
                                //QueryAllocationModel reqGetRealtimeAllocation = new();
                                //reqGetRealtimeAllocation.DistributorCode = distributorCode;
                                //reqGetRealtimeAllocation.WarehouseCode = wareHouseCode;
                                //reqGetRealtimeAllocation.LocationCode = LocationCodeAllowBoook.ToString();
                                //reqGetRealtimeAllocation.ItemCode = skuStandar.InventoryCode;

                                //ResultModelWithObject<INV_AllocationDetail> resAllocationDetailCurrent = await _inventoryService.GetAllocationDetailCurrent(reqGetRealtimeAllocation);

                                //if (!resAllocationDetailCurrent.IsSuccess)
                                //{
                                //    return new BaseResultModel
                                //    {
                                //        IsSuccess = false,
                                //        Code = resAllocationDetailCurrent.Code,
                                //        Message = resAllocationDetailCurrent.Message
                                //    };
                                //}

                                //INV_AllocationDetail allocationDetailCurrent = resAllocationDetailCurrent.Data;

                                // Số sales qty có thể booked
                                var allocationAllowToBook = (int)(allocationDetailCurrent.Available / item.UnitRate);

                                //Sales Qty cần book cho Sku này
                                var skuSaleQtyNeedBook = skuSalesAvailableQty > remainNeedBook ? remainNeedBook : skuSalesAvailableQty;
                                // check allocation khi quy ra sales phải ít nhất là 1
                                if (allocationAllowToBook > 1)
                                {
                                    var SalesBookedQty = allocationAllowToBook > skuSaleQtyNeedBook ? skuSaleQtyNeedBook : allocationAllowToBook;

                                    if (SalesBookedQty > 0)
                                    {

                                        //// Get detail SKU
                                        //var detailSKU = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryItemByCode/{skuStandar.InventoryCode}", Method.GET, $"Rdos {token.Split(" ").Last()}", null);
                                        // Handle SKU
                                        var detailSKU = _listInventoryItem.FirstOrDefault(x => x.InventoryItem.InventoryItemId == skuStandar.InventoryCode);

                                        // Nếu detail SKU null
                                        if (detailSKU == null)
                                        {
                                            // Get detail inventory item
                                            detailSKU = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryItemByCode/{skuStandar.InventoryCode}", Method.GET, $"Rdos {token.Split(" ").Last()}", null);
                                            _listInventoryItem.Add(detailSKU);
                                        }

                                        // Get attribute code của SKU
                                        var detailSelectedItem = _clientService.CommonRequest<ItemMng_InventoryItemAttribute>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryAttributeByCode/{skuStandar.InventoryCode}", Method.GET, $"Rdos {token.Split(" ").Last()}", null);

                                        // Không tìm thấy data detail thì skip
                                        if (detailSKU == null || detailSelectedItem == null)
                                        {
                                            //đánh 2 cờ Need Confirm
                                            _stockImport = false;
                                            _stockImportAll = false;
                                            continue;
                                        }

                                        var requestBook = new UpdateBookedAllocationModel
                                        {
                                            DistributorCode = distributorCode,
                                            WareHouseCode = wareHouseCode,
                                            LocationCode = allocationDetailCurrent.LocationCode,
                                            ItemCode = detailSKU.InventoryItem.InventoryItemId,
                                            SOBooked = (int)(SalesBookedQty * item.UnitRate), //Quy lại base,
                                        };

                                        BookAllocationModel reqBook = new();
                                        reqBook.OrderID = FFAOrderId;
                                        reqBook.OneShopID = null;
                                        reqBook.FFAVisitID = _ffaVisitId;
                                        reqBook.CreatedBy = _createdBy;
                                        reqBook.BookBaseQty = requestBook.SOBooked;
                                        reqBook.BookQty = SalesBookedQty;
                                        reqBook.BookUom = item.UOM;
                                        reqBook.ItemGroupCode = item.ItemGroupCode;
                                        reqBook.Priority = count;
                                        reqBook.OrderLineId = item.Id.ToString();
                                        reqBook.OrderType = item.OrderType;

                                        var resBooked = await UpdateBooked(requestBook, reqBook);
                                        if (!resBooked.IsSuccess)
                                            return resBooked;


                                        int _BookedQty = SalesBookedQty;
                                        int _BookedBaseQty = (int)(SalesBookedQty * item.UnitRate);
                                        // Handle lại total qty nếu có transaction trước đó
                                        foreach (var tranDetail in listInvTransactionByItemGroup.Where(x => x.ItemCode == detailSKU.InventoryItem.InventoryItemId).ToList())
                                        {
                                            _BookedQty += tranDetail.Quantity;
                                            _BookedBaseQty += tranDetail.BaseQuantity;
                                            tranDetail.IsCreateOrderItem = true;
                                        }

                                        var itemNew = mappingItemFromFFaItem(item);
                                        itemNew.OriginalOrderQuantities = _BookedQty;
                                        itemNew.OriginalOrderBaseQuantities = _BookedBaseQty;
                                        itemNew.OrderQuantities = _BookedQty;
                                        itemNew.OrderBaseQuantities = _BookedBaseQty;
                                        //Bổ sung thuộc tính cho Item
                                        itemNew.Id = Guid.NewGuid();
                                        itemNew.ItemCode = skuStandar.InventoryCode;
                                        itemNew.InventoryID = skuStandar.InventoryCode;
                                        itemNew.ItemDescription = skuStandar.InventoryDescription;
                                        itemNew.ItemGroupCode = skuStandar.ItemGroupCode;
                                        itemNew.BaseUnit = detailSKU.InventoryItem.BaseUnit;
                                        itemNew.BaseUnitCode = detailSelectedItem.BaseUOMCode;
                                        itemNew.BaseUomCode = detailSelectedItem.BaseUOMCode;
                                        itemNew.ItemId = detailSKU.InventoryItem.Id;
                                        itemNew.PurchaseUnit = detailSKU.InventoryItem.PurchaseUnit;
                                        itemNew.PurchaseUnitCode = detailSelectedItem.PurchaseUOMCode;
                                        itemNew.SalesUnit = detailSKU.InventoryItem.SalesUnit;
                                        itemNew.SalesUnitCode = detailSelectedItem.SalesUOMCode;
                                        itemNew.VatId = detailSKU.InventoryItem.Vat;
                                        itemNew.Hierarchy = detailSKU.InventoryItem.Hierarchy;
                                        itemNew.InventoryAttibute1 = detailSelectedItem.AttributeCode1;
                                        itemNew.InventoryAttibute2 = detailSelectedItem.AttributeCode2;
                                        itemNew.InventoryAttibute3 = detailSelectedItem.AttributeCode3;
                                        itemNew.InventoryAttibute4 = detailSelectedItem.AttributeCode4;
                                        itemNew.InventoryAttibute5 = detailSelectedItem.AttributeCode5;
                                        itemNew.InventoryAttibute6 = detailSelectedItem.AttributeCode6;
                                        itemNew.InventoryAttibute7 = detailSelectedItem.AttributeCode7;
                                        itemNew.InventoryAttibute8 = detailSelectedItem.AttributeCode8;
                                        itemNew.InventoryAttibute9 = detailSelectedItem.AttributeCode9;
                                        itemNew.InventoryAttibute10 = detailSelectedItem.AttributeCode10;
                                        itemNew.LocationID = allocationDetailCurrent.LocationCode;
                                        item.LocationID = allocationDetailCurrent.LocationCode;

                                        //THông tin order
                                        itemNew.LocationID = allocationDetailCurrent.LocationCode;
                                        itemNew.OrderQuantities = SalesBookedQty;
                                        itemNew.UOM = item.UOM;

                                        // handle data Price
                                        itemNew.Orig_Ord_Line_Amt = itemNew.UnitPrice * itemNew.OrderQuantities;
                                        itemNew.Ord_Line_Amt = itemNew.UnitPrice * itemNew.OrderQuantities;
                                        itemNew.Shipped_Line_Amt = 0;

                                        if (item.Orig_Ord_line_Disc_Amt.HasValue && item.OriginalOrderQty.HasValue)
                                        {
                                            itemNew.Orig_Ord_line_Disc_Amt = (decimal)item.Orig_Ord_line_Disc_Amt.Value * itemNew.OrderQuantities / (decimal)item.OriginalOrderQty.Value;
                                        }
                                        else
                                        {
                                            itemNew.Orig_Ord_line_Disc_Amt = 0;
                                        }

                                        itemNew.Ord_line_Disc_Amt = itemNew.Orig_Ord_line_Disc_Amt;
                                        itemNew.Shipped_line_Disc_Amt = 0;
                                        itemNew.Orig_Ord_Line_Extend_Amt = itemNew.Orig_Ord_Line_Amt - itemNew.Orig_Ord_line_Disc_Amt;
                                        itemNew.Ord_Line_Extend_Amt = itemNew.Ord_Line_Amt - itemNew.Ord_line_Disc_Amt;
                                        itemNew.Shipped_Line_Extend_Amt = 0;

                                        // Add item
                                        listItemSkuNew.Add(itemNew);
                                    }

                                    //Tính lại số remain cần book
                                    remainNeedBook -= SalesBookedQty;
                                    remainBooked += SalesBookedQty;
                                    item.OriginalOrderQtyBooked += SalesBookedQty;
                                }
                            }

                            if (item.OriginalOrderQtyBooked != item.OriginalOrderQty)
                            {
                                _stockImport = false;
                                _stockImportAll = false;
                            }
                        }
                        else
                        {
                            _stockImport = false;
                            _stockImportAll = false;
                        }
                    }

                    // TH3: Đã book đủ kho ItemGroup
                    if (_stockImport)
                    {
                        // List transaction chưa map thành item trong SO Order
                        List<INV_InventoryTransaction> _listHandleCreateOrderItem = _listInvTransactionByVisitId
                            .Where(x => x.ItemGroupCode != null 
                                && x.ItemGroupCode == item.ItemGroupCode 
                                && !x.IsCreateOrderItem
                                && x.OrderLineId == item.Id.ToString())
                            .ToList();
                        foreach (var itemInvGroup in _listHandleCreateOrderItem.GroupBy(x => x.ItemCode).Select(x => x.First()).ToList())
                        {
                            // Handle SKU
                            var detailSKU = _listInventoryItem.FirstOrDefault(x => x.InventoryItem.InventoryItemId == itemInvGroup.ItemCode);

                            // Nếu detail SKU null
                            if (detailSKU == null)
                            {
                                // Get detail inventory item
                                detailSKU = _clientService.CommonRequest<ItemMng_IventoryItemModel>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryItemByCode/{itemInvGroup.ItemCode}", Method.GET, $"Rdos {token.Split(" ").Last()}", null);
                                _listInventoryItem.Add(detailSKU);
                            }

                            // Get attribute code của SKU
                            var detailSelectedItem = _clientService.CommonRequest<ItemMng_InventoryItemAttribute>(CommonData.SystemUrlCode.ODItemAPI, $"InventoryItem/GetInventoryAttributeByCode/{itemInvGroup.ItemCode}", Method.GET, $"Rdos {token.Split(" ").Last()}", null);

                            // Không tìm thấy data detail thì skip
                            if (detailSKU == null || detailSelectedItem == null)
                            {
                                //đánh 2 cờ Need Confirm
                                _stockImport = false;
                                _stockImportAll = false;
                                continue;
                            }

                            int _Quantity = 0;
                            int _BaseQty = 0;
                            foreach (var itemInv in _listHandleCreateOrderItem.Where(x => x.ItemCode == itemInvGroup.ItemCode).ToList())
                            {
                                _Quantity += itemInv.Quantity;
                                _BaseQty += itemInv.BaseQuantity;
                            }

                            var itemNew = mappingItemFromFFaItem(item);
                            //Bổ sung thuộc tính cho Item
                            itemNew.Id = Guid.NewGuid();
                            itemNew.ItemCode = itemInvGroup.ItemCode;
                            itemNew.InventoryID = itemInvGroup.ItemCode;
                            itemNew.ItemDescription = itemInvGroup.ItemDescription;
                            itemNew.ItemGroupCode = itemInvGroup.ItemGroupCode;
                            itemNew.BaseUnit = detailSKU.InventoryItem.BaseUnit;
                            itemNew.BaseUnitCode = detailSelectedItem.BaseUOMCode;
                            itemNew.BaseUomCode = detailSelectedItem.BaseUOMCode;
                            itemNew.ItemId = detailSKU.InventoryItem.Id;
                            itemNew.PurchaseUnit = detailSKU.InventoryItem.PurchaseUnit;
                            itemNew.PurchaseUnitCode = detailSelectedItem.PurchaseUOMCode;
                            itemNew.SalesUnit = detailSKU.InventoryItem.SalesUnit;
                            itemNew.SalesUnitCode = detailSelectedItem.SalesUOMCode;
                            itemNew.VatId = detailSKU.InventoryItem.Vat;
                            itemNew.Hierarchy = detailSKU.InventoryItem.Hierarchy;
                            itemNew.InventoryAttibute1 = detailSelectedItem.AttributeCode1;
                            itemNew.InventoryAttibute2 = detailSelectedItem.AttributeCode2;
                            itemNew.InventoryAttibute3 = detailSelectedItem.AttributeCode3;
                            itemNew.InventoryAttibute4 = detailSelectedItem.AttributeCode4;
                            itemNew.InventoryAttibute5 = detailSelectedItem.AttributeCode5;
                            itemNew.InventoryAttibute6 = detailSelectedItem.AttributeCode6;
                            itemNew.InventoryAttibute7 = detailSelectedItem.AttributeCode7;
                            itemNew.InventoryAttibute8 = detailSelectedItem.AttributeCode8;
                            itemNew.InventoryAttibute9 = detailSelectedItem.AttributeCode9;
                            itemNew.InventoryAttibute10 = detailSelectedItem.AttributeCode10;
                            itemNew.LocationID = itemInvGroup.LocationCode;
                            item.LocationID = itemInvGroup.LocationCode;

                            //THông tin order
                            itemNew.OrderQuantities = _Quantity;
                            itemNew.UOM = item.UOM;

                            // handle data Price
                            itemNew.Orig_Ord_Line_Amt = itemNew.UnitPrice * itemNew.OrderQuantities;
                            itemNew.Ord_Line_Amt = itemNew.UnitPrice * itemNew.OrderQuantities;
                            itemNew.Shipped_Line_Amt = 0;

                            if (item.Orig_Ord_line_Disc_Amt.HasValue && item.OriginalOrderQty.HasValue)
                            {
                                itemNew.Orig_Ord_line_Disc_Amt = (decimal)item.Orig_Ord_line_Disc_Amt.Value * itemNew.OrderQuantities / (decimal)item.OriginalOrderQty.Value;
                            }
                            else
                            {
                                itemNew.Orig_Ord_line_Disc_Amt = 0;
                            }

                            itemNew.Ord_line_Disc_Amt = itemNew.Orig_Ord_line_Disc_Amt;
                            itemNew.Shipped_line_Disc_Amt = 0;
                            itemNew.Orig_Ord_Line_Extend_Amt = itemNew.Orig_Ord_Line_Amt - itemNew.Orig_Ord_line_Disc_Amt;
                            itemNew.Ord_Line_Extend_Amt = itemNew.Ord_Line_Amt - itemNew.Ord_line_Disc_Amt;
                            itemNew.Shipped_Line_Extend_Amt = 0;
                            itemNew.OriginalOrderQuantities = itemNew.OrderQuantities;
                            itemNew.OriginalOrderBaseQuantities = itemNew.OrderBaseQuantities;
                            listItemSkuNew.Add(itemNew);
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
        // Mapping model item
        private SO_OrderItems mappingItemFromFFaItem(FfasoOrderItem item)
        {
            var itemNew = _mapper.Map<SO_OrderItems>(item);
            itemNew.Id = Guid.NewGuid();
            itemNew.ItemId = ItemId;
            itemNew.BaseUnit = BaseUnit;
            itemNew.Hierarchy = Hierarchy;
            itemNew.PurchaseUnit = PurchaseUnit;
            itemNew.SalesUnit = SalesUnit;
            itemNew.VatId = VatId;
            itemNew.ItemGroupCode = ItemGroupCode;
            itemNew.KitId = KitId;
            itemNew.Ord_Line_TotalBeforeTax_Amt = item.Orig_Ord_Line_TaxBefore_Amt;
            itemNew.Ord_Line_TotalAfterTax_Amt = item.Orig_Ord_Line_TaxAfter_Amt;
            itemNew.Ord_TotalLine_Disc_Amt = item.Orig_Ord_TotalLine_Disc_Amt;
            itemNew.UnitPriceBeforeTax = item.UnitPriceBeforeTax;
            itemNew.UnitPriceAfterTax = item.UnitPriceAfterTax;
            return itemNew;
        }
        // Calculate book budget
        public async Task<BaseResultModel> HandleCalculateBudget(List<FfaOrderGroupModel> listInput, string token, string username, List<IssueImportOrderModel> listSuccess, List<IssueImportOrderModel> listErrors)
        {
            try
            {
                _createdBy = username;
                // Get list location
                var principalLocationList = await GetListPrincipalWarehouseLocation();
                if (principalLocationList.IsSuccess)
                {
                    principalLocations = principalLocationList.Data;
                    locationDefault = principalLocations.FirstOrDefault(x => x.IsDefault);
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

                var principal = await _principalRepository.GetAllQueryable().AsNoTracking().FirstOrDefaultAsync();
                if (principal != null)
                {
                    CountryPrincipal = principal.Country;
                }

                // Get list VAT
                _listVats = await _vatRepository.GetAllQueryable().AsNoTracking().ToListAsync();

                //Get salesPriceIncludeVaT Settings
                bool _salesPriceIncludeVaT = _calculateTaxService.GetSalesPriceIncludeVaT();

                var listBudgetInProcess = new List<Temp_SOBudgets>();
                // Loop order
                foreach (var order in listInput)
                {
                    _listInvTransactionByVisitId = new List<INV_InventoryTransaction>();
                    trackingId = Guid.NewGuid();
                    _ffaVisitId = order.OrderInfo.VisitID;
                    FFAOrderId = order.OrderInfo.Id;
                    _stockImportAll = true;
                    _importBudgetStatus = true;

                    // Generate RefNumber
                    var prefix = StringsHelper.GetPrefixYYM();
                    var orderRefNumberIndb = await _orderInformationsRepository.GetAllQueryable(null, null, null, _schemaName)
                        .Where(x => x.OrderRefNumber.StartsWith(prefix))
                        .AsNoTracking().Select(x => x.OrderRefNumber).OrderByDescending(x => x).FirstOrDefaultAsync();

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
                    order.OrderInfo.ImportStatus = IMPORT_STATUS.SUCCESS;
                    // Get all list transaction of OneShopId
                    _listInvTransactionByVisitId = await _inventoryService.GetTransactionsByFfaVisitId(_ffaVisitId, order.OrderInfo.OrderType);
                    // Kiểm tra transaction trước đó có bị book âm hay không
                    BaseResultModel checkBookedFromInvTransacion = await CheckInvTransactionNegative(_listInvTransactionByVisitId);
                    //if (!checkBookedFromInvTransacion.IsSuccess) return checkBookedFromInvTransacion;
                    if (!checkBookedFromInvTransacion.IsSuccess)
                    {
                        listErrors.Add(new IssueImportOrderModel
                        {
                            OrderRefNumber = order.OrderInfo.External_OrdNBR,
                            Message = checkBookedFromInvTransacion.Message
                        });
                        continue;
                    }

                    // Loop item of order
                    foreach (var item in order.Items)
                    {
                        _stockImport = true;
                        if(item.OriginalOrderQty == 0) continue;
                        if(item.OriginalOrderBaseQty == 0) continue;

                        // Handle flow budget
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
                                budgetRequest.customerCode = order.OrderInfo.CustomerId;
                                budgetRequest.customerShipTo = order.OrderInfo.CustomerShiptoID;
                                budgetRequest.saleOrg = order.OrderInfo.SalesOrgID;
                                // budgetRequest.budgetAllocationLevel = "DSA"; // đang hardcode temp theo yêu cầu PO NAM
                                budgetRequest.budgetAllocationLevel = null;
                                budgetRequest.budgetBook = _budgetNeedBook;
                                //budgetRequest.salesTerritoryValueCode = order.OrderInfo.TerritoryValueKey;
                                budgetRequest.promotionCode = item.PromotionCode;
                                budgetRequest.promotionLevel = item.PromotionLevelCode;
                                budgetRequest.routeZoneCode = order.OrderInfo.RouteZoneID;
                                budgetRequest.dsaCode = order.OrderInfo.DSAID;
                                budgetRequest.subAreaCode = order.OrderInfo.SubAreaId;
                                budgetRequest.areaCode = order.OrderInfo.AreaId;
                                budgetRequest.subRegionCode = order.OrderInfo.SubRegionId;
                                budgetRequest.regionCode = order.OrderInfo.RegionId;
                                budgetRequest.branchCode = order.OrderInfo.BranchId;
                                budgetRequest.nationwideCode = CountryPrincipal;
                                budgetRequest.salesOrgCode = order.OrderInfo.SalesOrgID;
                                budgetRequest.referalCode = order.OrderInfo.External_OrdNBR;
                                budgetRequest.distributorCode = _distributorCode;

                                var budgetBookResult = (_clientService.CommonRequest<ResultModelWithObject<BudgetResponseModel>>(CommonData.SystemUrlCode.ODTpAPI, $"external_checkbudget/checkbudget", Method.POST, token, budgetRequest, true)).Data;

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

                        // Handle Book inventory
                        item.StockImport = null;

                        if (item.BudgetImport == null || item.BudgetImport.HasValue && item.BudgetImport.Value)
                        {
                            listItemSkuNew = new List<SO_OrderItems>();

                            // Handle book inventory
                            var resultHandleBookInventory = await HandleBookInventoryV2(item, order.OrderInfo.DistributorCode, order.OrderInfo.WareHouseID, token, order.ItemImports);
                            //if (!resultHandleBookInventory.IsSuccess)
                            //    return resultHandleBookInventory;

                            if (!resultHandleBookInventory.IsSuccess)
                            {
                                _stockImport = false;
                                _stockImportAll = false;
                                listErrors.Add(new IssueImportOrderModel
                                {
                                    OrderRefNumber = order.OrderInfo.External_OrdNBR,
                                    Message = resultHandleBookInventory.Message
                                });
                                break;
                            }

                            if (item.OriginalOrderQtyBooked < 1) {
                                _stockImport = false;
                                _stockImportAll = false;
                            };

                            // Cập nhật cờ stock import
                            item.StockImport = _stockImport;

                            if (item.StockImport.Value)
                            {
                                if (listItemSkuNew.Count > 0)
                                {

                                    foreach (var itemSkuNew in listItemSkuNew)
                                    {
                                        itemSkuNew.OrderRefNumber = generatedNumber;
                                    }

                                    orderItemNews.AddRange(listItemSkuNew);
                                }
                                else
                                {
                                    var itemNew = _mapper.Map<SO_OrderItems>(item);
                                    itemNew.Id = Guid.NewGuid();
                                    itemNew.ItemId = ItemId;
                                    itemNew.BaseUnit = BaseUnit;
                                    itemNew.Hierarchy = Hierarchy;
                                    itemNew.PurchaseUnit = PurchaseUnit;
                                    itemNew.SalesUnit = SalesUnit;
                                    itemNew.VatId = VatId;
                                    itemNew.ItemGroupCode = ItemGroupCode;
                                    itemNew.OrderRefNumber = generatedNumber;
                                    itemNew.KitId = KitId;
                                    itemNew.LocationID = LocationCodeAllowBoook.ToString();
                                    orderItemNews.Add(itemNew);
                                    item.LocationID = LocationCodeAllowBoook.ToString();
                                }
                            }
                        }

                        // Update transaction
                        _ffaSoOrderItemRepository.UpdateUnSaved(item, _schemaName);
                    } // End Order

                    // Handle status FFASOSTATUS
                    bool _waittingStock = order.OrderInfo.WaittingStock.HasValue ? order.OrderInfo.WaittingStock.Value : false;
                    bool _waitingBudget = order.OrderInfo.WaittingBudget.HasValue ? order.OrderInfo.WaittingBudget.Value : false;

                    // Validate import status
                    if (_importBudgetStatus != null && !_importBudgetStatus.Value && _stockImportAll)
                    {
                        if (!_waitingBudget)
                        {
                            order.OrderInfo.Status = FFASOSTATUS.NeedConfirm;
                        }
                        else
                        {
                            order.OrderInfo.Status = FFASOSTATUS.WatingBudget;
                        }
                        order.OrderInfo.ImportStatus = IMPORT_STATUS.FAILED;
                    }
                    else if (_importBudgetStatus != null && _importBudgetStatus.Value && !_stockImportAll)
                    {
                        if (!_waittingStock)
                        {
                            order.OrderInfo.Status = FFASOSTATUS.NeedConfirm;

                        }
                        else
                        {
                            order.OrderInfo.Status = FFASOSTATUS.WatingStock;
                        }

                        order.OrderInfo.ImportStatus = IMPORT_STATUS.FAILED;
                    }
                    else if (_importBudgetStatus != null && !_importBudgetStatus.Value && !_stockImportAll)
                    {
                        if (!_waitingBudget)
                        {
                            order.OrderInfo.Status = FFASOSTATUS.NeedConfirm;
                        }
                        else
                        {
                            order.OrderInfo.Status = FFASOSTATUS.WatingBudget;
                        }
                        order.OrderInfo.ImportStatus = IMPORT_STATUS.FAILED;
                    }
                    else if (_importBudgetStatus == null && !_stockImportAll)
                    {
                        if (!_waittingStock)
                        {
                            order.OrderInfo.Status = FFASOSTATUS.NeedConfirm;
                        }
                        else
                        {
                            order.OrderInfo.Status = FFASOSTATUS.WatingStock;
                        }
                        order.OrderInfo.ImportStatus = IMPORT_STATUS.FAILED;
                    }
                    else
                    {
                        order.OrderInfo.Status = FFASOSTATUS.ImportSuccessfully;
                    }

                    if (order.OrderInfo.ImportStatus == IMPORT_STATUS.SUCCESS)
                    {
                        // Map order header
                        orderNew = _mapper.Map<SaleOrderModel>(order.OrderInfo);
                        orderNew.ExpectDeliveryNote = order.OrderInfo.DeliveryTime;
                        orderNew.ReferenceRefNbr = order.OrderInfo.External_OrdNBR;
                        orderNew.Ord_TotalBeforeTax_Amt = order.OrderInfo.Orig_Ord_TotalBeforeTax_Amt;
                        orderNew.Ord_TotalAfterTax_Amt = order.OrderInfo.Orig_Ord_TotalAfterTax_Amt;

                        // handle data KIT
                        if (order.Items.FirstOrDefault(x => x.IsKit.HasValue && x.IsKit.Value) != null)
                        {
                            var groupKits = order.Items.GroupBy(x => new { x.IsKit.Value, x.KitId }).Select(x => x.First()).Where(x => x.KitId != null).ToList();
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
                                    itemKit.LocationID = locationDefault.Code.ToString();
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
                                    itemKit.PromotionBudgetCode = kitNew.BudgetCode;
                                    itemKit.PromotionBudgetQuantities = kitNew.BudgetBooked;

                                    orderItemNews.Add(itemKit);
                                }
                            }
                        }

                        // Gán list item cho Order
                        orderNew.OrderItems = orderItemNews;

                        var resultCreateSOOrder = await _salesOrderService.InsertOrderFromFFA(orderNew, token, username, generatedNumber, _listVats, _listInvTransactionByVisitId, _salesPriceIncludeVaT);
                        //if (!resultCreateSOOrder.IsSuccess) return resultCreateSOOrder;
                        if (!resultCreateSOOrder.IsSuccess)
                        {
                            listErrors.Add(new IssueImportOrderModel
                            {
                                OrderRefNumber = order.OrderInfo.External_OrdNBR,
                                Message = resultCreateSOOrder.Message
                            });
                            continue;
                        }

                        order.OrderInfo.OrderRefNumber = generatedNumber;

                        // Update order ref number item ffa
                        foreach (var itemFfaUpdate in order.Items)
                        {
                            itemFfaUpdate.OrderRefNumber = generatedNumber;
                            _ffaSoOrderItemRepository.UpdateUnSaved(itemFfaUpdate, _schemaName);
                        }
                    }
                    else
                    {
                        if (order.OrderInfo.Status == FFASOSTATUS.NeedConfirm)
                        {
                            var requestNoti = new SendNotifiMobileModel();
                            requestNoti.EmployeeCode = order.OrderInfo.SalesRepID;
                            requestNoti.ExternalNumber = order.OrderInfo.External_OrdNBR;
                            requestNoti.CustomerName = order.OrderInfo.CustomerName;
                            await NotifyMobileOrderImportFailed(token, requestNoti);
                        }
                    }
                    order.OrderInfo.UpdatedDate = DateTime.Now;
                    _ffaSoOrderRepository.UpdateUnSaved(order.OrderInfo, _schemaName);
                    _ffaSoOrderRepository.Save(_schemaName);
                    listSuccess.Add(new IssueImportOrderModel
                    {
                        OrderRefNumber = order.OrderInfo.OrderRefNumber,
                        Message = "SUCCESSFULLY"
                    });
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
        // Import all order
        public async Task<BaseResultModel> ImportAllOrder(string token, string username, SearchFfaOrderModel req)
        {
            try
            {
                // Get list order ffa
                req.IsDropdown = true;
                req.FromDate = null;
                req.ToDate = null;
                req.Filters = null;
                List<IssueImportOrderModel> listSuccess = new List<IssueImportOrderModel>();
                List<IssueImportOrderModel> listError = new List<IssueImportOrderModel>();
                var resultGetListOrderFfa = await GetListOrderFfa(req);

                if (!resultGetListOrderFfa.IsSuccess)
                {
                    return new BaseResultModel()
                    {
                        Code = resultGetListOrderFfa.Code,
                        Message = resultGetListOrderFfa.Message,
                        IsSuccess = false
                    };
                }

                var listOrderFfa = resultGetListOrderFfa.Data.Items;



                // Handle flow budget
                var resultHandleBudget = await HandleCalculateBudget(listOrderFfa, token, username, listSuccess, listError);
                if (!resultHandleBudget.IsSuccess) return resultHandleBudget;

                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = new IssueImportResultModel
                    {
                        ListSuccess = listSuccess,
                        ListError = listError
                    }
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
        public async Task<BaseResultModel> ImportListOrder(ImportListFfaOrder dataInput, string token, string username)
        {
            try
            {
                List<IssueImportOrderModel> listSuccess = new List<IssueImportOrderModel>();
                List<IssueImportOrderModel> listError = new List<IssueImportOrderModel>();

                var listOrderFfa = new List<FfaOrderGroupModel>();
                foreach (var orderNumber in dataInput.OrderRefNumbers)
                {
                    var duplicated = await _ffaSoOrderRepository.GetAllQueryable(x => x.External_OrdNBR == orderNumber 
                        && x.OrderType != FFA_ORDER_TYPE.DirectOrder, null, null, _schemaName)
                        .CountAsync();

                    // Lỗi trùng mã đơn
                    if (duplicated > 1)
                    {
                        listError.Add(new IssueImportOrderModel
                        {
                            OrderRefNumber = orderNumber,
                            Message = $"Đơn hàng mã {orderNumber} bị trùng. Vui lòng kiểm tra lại"
                        });
                        continue;
                    }

                    // Lỗi lấy chi tiết đơn hàng
                    var orderFfa = await GetDetailFfaOrder(orderNumber);
                    if (!orderFfa.IsSuccess)
                    {
                        listError.Add(new IssueImportOrderModel
                        {
                            OrderRefNumber = orderNumber,
                            Message = orderFfa.Message
                        });
                        continue;
                    }

                    // Lỗi đơn hàng không có item
                    if (orderFfa.Data.Items.Count == 0)
                    {
                        listError.Add(new IssueImportOrderModel
                        {
                            OrderRefNumber = orderNumber,
                            Message = $"Đơn hàng mã {orderNumber} không tìm thấy danh sách sản phẩm"
                        });
                        continue;
                    }

                    // Đơn hợp lệ
                    listOrderFfa.Add(orderFfa.Data);
                }

                // Xử lý tạo đơn SO với những đơn FFA hợp lệ
                if (listOrderFfa.Count > 0)
                {
                    // Handle flow budget
                    var resultHandleBudget = await HandleCalculateBudget(listOrderFfa, token, username, listSuccess, listError);
                    // Lỗi hệ thống không thể tiếp tục
                    if (!resultHandleBudget.IsSuccess) return resultHandleBudget;
                }

                // Đã xử lý xong
                return new BaseResultModel
                {
                    IsSuccess = true,
                    Code = 200,
                    Message = "Success",
                    Data = new IssueImportResultModel
                    {
                        ListSuccess = listSuccess,
                        ListError = listError
                    }
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

        public async Task<BaseResultModel> CancelFFAOrders(ImportListFfaOrder dataInput, string token, string username)
        {
            try
            {
                //FFA flow
                var ffaOrder = await _ffaSoOrderRepository.GetAllQueryable(x =>
                    dataInput.OrderRefNumbers.Contains(x.External_OrdNBR)
                    && x.Status != FFASOSTATUS.ImportSuccessfully
                    && x.Status != FFASOSTATUS.CanCelImport
                    && x.OrderType != FFA_ORDER_TYPE.DirectOrder,
                    null, null, _schemaName)
                    .ToListAsync();

                foreach (var order in ffaOrder)
                {
                    // Get list transaction from INV
                    List<INV_InventoryTransaction> _listInvTransactionByVisitId = await _inventoryService.GetTransactionsByFfaVisitId(order.VisitID, order.OrderType);
                    if (_listInvTransactionByVisitId.Count > 0)
                    {
                        foreach (var item in _listInvTransactionByVisitId)
                        {
                            var resCancelBook = await _inventoryService.CancelBookedFFAOrder(item, username);
                            if (!resCancelBook.IsSuccess) return resCancelBook;
                        }
                    }

                    var ffaOrderItems = await _ffaSoOrderItemRepository.GetAllQueryable(x =>
                        x.External_OrdNBR == order.External_OrdNBR
                        && x.VisitId == order.VisitID
                        && !string.IsNullOrEmpty(x.BudgetCode), null, null, _schemaName).ToListAsync();

                    foreach (var orderItem in ffaOrderItems)
                    {
                        await _salesOrderService.HandleCancelBudgetFFA(orderItem, order, token);
                    }
                    order.Status = FFASOSTATUS.CanCelImport;
                    order.ReasonCode = BL_CANCEL_REASON_CODE;
                    _ffaSoOrderRepository.UpdateUnSaved(order, _schemaName);
                }
                _orderInformationsRepository.Save(_schemaName);
                return new BaseResultModel
                {
                    Code = 200,
                    Message = "OK",
                    IsSuccess = true,
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


        public async Task<BaseResultModel> CancelFFAOrdersV2(List<CancelListFfaOrder> dataInput, string token, string username)
        {
            try
            {
                //FFA flow
                var dataInputSet = new HashSet<string>(dataInput.Select(x => x.OrderRefNumbers));
                var ffaOrder = await _ffaSoOrderRepository.GetAllQueryable(x =>
                    dataInputSet.Contains(x.External_OrdNBR)
                    && x.Status != FFASOSTATUS.ImportSuccessfully
                    && x.Status != FFASOSTATUS.CanCelImport
                    && x.OrderType != FFA_ORDER_TYPE.DirectOrder,
                    null, null, _schemaName)
                    .ToListAsync();

                foreach (var order in ffaOrder)
                {
                    // Get list transaction from INV
                    List<INV_InventoryTransaction> _listInvTransactionByVisitId = await _inventoryService.GetTransactionsByFfaVisitId(order.VisitID, order.OrderType);
                    if (_listInvTransactionByVisitId.Count > 0)
                    {
                        foreach (var item in _listInvTransactionByVisitId)
                        {
                            var resCancelBook = await _inventoryService.CancelBookedFFAOrder(item, username);
                            if (!resCancelBook.IsSuccess) return resCancelBook;
                        }
                    }

                    var ffaOrderItems = await _ffaSoOrderItemRepository.GetAllQueryable(x =>
                        x.External_OrdNBR == order.External_OrdNBR
                        && x.VisitId == order.VisitID
                        && !string.IsNullOrEmpty(x.BudgetCode), null, null, _schemaName).ToListAsync();

                    foreach (var orderItem in ffaOrderItems)
                    {
                        await _salesOrderService.HandleCancelBudgetFFA(orderItem, order, token);
                    }
                    order.Status = FFASOSTATUS.CanCelImport;
                    order.ReasonCode = BL_CANCEL_REASON_CODE;
                    order.ReasonCancel = dataInput.Where(x => x.OrderRefNumbers == order.External_OrdNBR).Select(x => x.ReasonCancel).First();
                    _ffaSoOrderRepository.UpdateUnSaved(order, _schemaName);
                }
                _orderInformationsRepository.Save(_schemaName);
                return new BaseResultModel
                {
                    Code = 200,
                    Message = "OK",
                    IsSuccess = true,
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
